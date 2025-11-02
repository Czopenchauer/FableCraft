import logging
import os
from contextlib import asynccontextmanager
from datetime import datetime
from typing import Optional

import fastapi
import uvicorn
from fastapi import FastAPI, HTTPException
from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from pydantic import BaseModel
from starlette import status

from graphiti_core import Graphiti
from graphiti_core.cross_encoder.bge_reranker_client import BGERerankerClient
from graphiti_core.driver.neo4j_driver import Neo4jDriver
from graphiti_core.embedder import OpenAIEmbedder, OpenAIEmbedderConfig
from graphiti_core.errors import NodeNotFoundError
from graphiti_core.llm_client import LLMConfig
from graphiti_core.llm_client.openai_generic_client import OpenAIGenericClient
from graphiti_core.nodes import EpisodeType
from graphiti_core.utils.maintenance.graph_data_operations import clear_data

otlpExporter = OTLPSpanExporter()
processor = BatchSpanProcessor(otlpExporter)
tracerProvider = TracerProvider()
tracerProvider.add_span_processor(processor)
trace.set_tracer_provider(tracerProvider)


@asynccontextmanager
async def lifespan(app: FastAPI):
    graphiti = build_graph_client()

    try:
        logger.info("Initializing Graphiti and building indices/constraints...")
        await graphiti.build_indices_and_constraints()
        logger.info("Graphiti initialized successfully")
        await graphiti.close()
    except Exception as e:
        logger.error(f"{type(e).__name__}: Error initializing Graphiti: {str(e)}")
        raise e
    finally:
        await graphiti.close()

    yield

app = fastapi.FastAPI(
    title="GraphRAG API",
    description="API for GraphRAG operations using Graphiti",
    version="1.0.0",
    lifespan=lifespan
)
FastAPIInstrumentor.instrument_app(app)

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)
tracer = trace.get_tracer(__name__)


class AddDataRequest(BaseModel):
    episode_type: str
    description: str
    content: str
    group_id: str
    node_id: str
    reference_time: Optional[datetime] = None


class DeleteRequest(BaseModel):
    episode_id: str


class HealthResponse(BaseModel):
    status: str


class MessageResponse(BaseModel):
    message: str


class SearchRequest(BaseModel):
    adventure_id: str
    query: str
    character_name: Optional[str] = None


class EpisodeResponse(BaseModel):
    uuid: str
    name: str
    group_id: str
    source: str
    source_description: str
    content: str
    created_at: datetime
    valid_at: datetime
    entity_edges: list[str]


def build_graph_client() -> Graphiti:
    neo4j_uri = os.environ.get('NEO4J_URI', 'bolt://localhost:7687')
    neo4j_user = os.environ.get('NEO4J_USER', 'neo4j')
    neo4j_password = os.environ.get('NEO4J_PASSWORD', 'SuperPassword')

    if not neo4j_uri or not neo4j_user or not neo4j_password:
        raise ValueError('NEO4J_URI, NEO4J_USER, and NEO4J_PASSWORD must be set')

    llm_config = LLMConfig(
        api_key=os.environ.get("LLM_API_KEY"),
        model=os.environ.get("LLM_MODEL"),
        small_model=os.environ.get("LLM_MODEL"),
        base_url=os.environ.get("LLM_ENDPOINT"),
        temperature=1,
        max_tokens=150000
    )

    ollama_embedder = OpenAIEmbedder(
        config=OpenAIEmbedderConfig(
            api_key="abc",
            embedding_model=os.environ.get("EMBEDDING_MODEL"),
            base_url=os.environ.get("EMBEDDING_ENDPOINT"),
        ))

    driver = Neo4jDriver(neo4j_uri, neo4j_user, neo4j_password, database="neo4j")
    graphiti = Graphiti(
        graph_driver=driver,
        tracer=tracer,
        trace_span_prefix='graphrag',
        llm_client=OpenAIGenericClient(config=llm_config),
        embedder=ollama_embedder,
        cross_encoder=BGERerankerClient())
    return graphiti


@app.post("/search")
async def search(request: SearchRequest):
    """Endpoint to search the graph database"""
    client = build_graph_client()
    try:
        results = await client.search(request.query, num_results=20)
        return results
    finally:
        await client.close()


@app.post("/add", status_code=status.HTTP_201_CREATED)
async def add_data(data: AddDataRequest):
    """Endpoint to add data to the graph database

    Request body should contain:
    - episode_type: str (one of: message, event, action, narration, etc.)
    - description: str (brief description of the episode)
    - content: str (the actual content to be added)
    - reference_time: str (optional, timestamp for the episode)
    """

    graphiti = build_graph_client()
    try:
        episode_type = EpisodeType.from_str(data.episode_type.lower())
    except KeyError:
        valid_types = [e.name for e in EpisodeType]
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Invalid episode_type. Must be one of: {', '.join(valid_types)}"
        )

    try:
        result = await graphiti.add_episode(
            name=data.description,
            episode_body=data.content,
            source=episode_type,
            uuid=data.node_id,
            source_description=data.description,
            reference_time=data.reference_time or datetime.now(),
            group_id=data.group_id
        )

        return result.episode.uuid

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"{type(e).__name__}: Error adding data: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to add data: {str(e)}"
        )
    finally:
        await graphiti.close()


@app.get("/episode/{episode_id}", response_model=EpisodeResponse)
async def get_episode(episode_id: str):
    """Endpoint to retrieve a specific episode by its ID"""
    client = build_graph_client()
    try:
        from graphiti_core.nodes import EpisodicNode
        episode = await EpisodicNode.get_by_uuid(client.driver, episode_id)
        return EpisodeResponse(
            uuid=episode.uuid,
            name=episode.name,
            group_id=episode.group_id,
            source=episode.source.value,
            source_description=episode.source_description,
            content=episode.content,
            created_at=episode.created_at,
            valid_at=episode.valid_at,
            entity_edges=episode.entity_edges
        )
    except NodeNotFoundError as e:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Episode not found: {str(e)}"
        )
    except Exception as e:
        logger.error(f"{type(e).__name__}: Error retrieving episode: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to retrieve episode: {str(e)}"
        )
    finally:
        await client.close()


@app.delete("/delete_data")
async def delete_data(episode_id: str):
    """Endpoint to delete data from the graph database"""
    client = build_graph_client()
    try:
        await client.remove_episode(episode_id)
    except NodeNotFoundError as e:
        raise HTTPException(
            status_code=status.HTTP_204_NO_CONTENT,
            detail=f"Node not found: {str(e)}"
        )
    except Exception as e:
        logger.error(f"{type(e).__name__}: Error deleting data: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to delete data: {str(e)}"
        )
    finally:
        await client.close()


@app.delete("/clear")
async def clear():
    client = build_graph_client()
    try:
        await clear_data(client.driver)
        await client.build_indices_and_constraints()
    finally:
        await client.close()

@app.get("/", response_model=MessageResponse)
async def root():
    """Root endpoint to verify API is running"""
    return {"message": "GraphRAG API is running"}


@app.get("/health", response_model=HealthResponse)
async def health():
    """Health check endpoint"""
    return {"status": "healthy"}


if __name__ == '__main__':
    port = int(os.environ.get('PORT', 8112))
    host = os.environ.get('HOST', '127.0.0.1')

    logger.info(f"Starting FastAPI server on {host}:{port}")
    uvicorn.run(
        app,
        host=host,
        port=port,
        reload=bool(os.environ.get('DEBUG', False))
    )
