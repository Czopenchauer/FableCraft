import logging
import os
from contextlib import asynccontextmanager
from datetime import datetime
from enum import Enum
from typing import Optional

import fastapi
import uvicorn
from fastapi import FastAPI, HTTPException, BackgroundTasks
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
from graphiti_core.search.search_config import SearchConfig, EdgeSearchConfig, EdgeSearchMethod, EdgeReranker, \
    NodeSearchConfig, NodeSearchMethod, NodeReranker
from graphiti_core.utils.maintenance.graph_data_operations import clear_data
from graphiti_search_agent import create_graph_search_agent, GraphSearchState

otlpExporter = OTLPSpanExporter()
processor = BatchSpanProcessor(otlpExporter)
tracerProvider = TracerProvider()
tracerProvider.add_span_processor(processor)
trace.set_tracer_provider(tracerProvider)

@asynccontextmanager
async def lifespan(application: FastAPI):
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
        embedding_dim=4096,
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

agent = create_graph_search_agent(graphiti)

class TaskStatus(str, Enum):
    PENDING = "pending"
    PROCESSING = "processing"
    COMPLETED = "completed"
    FAILED = "failed"


task_store = {}


class AddDataRequest(BaseModel):
    episode_type: str
    description: str
    content: str
    group_id: str
    task_id: str
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

class SearchResult(BaseModel):
    content: str


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


class TaskStatusResponse(BaseModel):
    task_id: str
    episode_id: Optional[str] = None
    status: TaskStatus
    message: Optional[str] = None
    created_at: datetime
    completed_at: Optional[datetime] = None
    error: Optional[str] = None



@app.post("/search")
async def search(request: SearchRequest):
    """Endpoint to search the graph database"""
    initial_state = GraphSearchState(
        user_query=request.query,
        group_id=request.adventure_id,
        search_depth=3,
        current_depth=0,
        max_search_results_per_round=15,
        edges=[],
        nodes=[],
        communities=[],
        explored_entities=[],
        search_queries=[],
        intermediate_syntheses=[],
        final_answer="",
        reflection_feedback=None,
        needs_regeneration=False,
        regeneration_count=0
    )

    result = await agent.ainvoke(initial_state)
    return SearchResult(content=result["final_answer"])


async def process_episode_addition(
    task_id: str,
    episode_type: EpisodeType,
    description: str,
    content: str,
    group_id: str,
    reference_time: datetime
):
    """Background task to process episode addition sequentially"""
    # Update status to processing
    task_store[task_id]["status"] = TaskStatus.PROCESSING

    try:
        result = await graphiti.add_episode(
            name=description,
            episode_body=content,
            source=episode_type,
            source_description=description,
            reference_time=reference_time,
            group_id=group_id
        )
        logger.info(f"Successfully added episode {result.episode.uuid} in background")

        # Update status to completed
        task_store[task_id]["status"] = TaskStatus.COMPLETED
        task_store[task_id]["episode_id"] = result.episode.uuid
        task_store[task_id]["message"] = f"Episode {result.episode.uuid} added successfully"
        task_store[task_id]["completed_at"] = datetime.now()

    except Exception as e:
        logger.error(f"{type(e).__name__}: Error adding episode in background: {str(e)}")

        # Update status to failed
        task_store[task_id]["status"] = TaskStatus.FAILED
        task_store[task_id]["error"] = f"{type(e).__name__}: {str(e)}"
        task_store[task_id]["completed_at"] = datetime.now()


@app.post("/add", status_code=status.HTTP_202_ACCEPTED)
async def add_data(data: AddDataRequest, background_tasks: BackgroundTasks):
    """Endpoint to add data to the graph database as a background task

    Request body should contain:
    - episode_type: str (one of: message, event, action, narration, etc.)
    - description: str (brief description of the episode)
    - content: str (the actual content to be added)
    - group_id: str (group identifier for the episode)
    - task_id: str (unique identifier for the episode)
    - reference_time: datetime (optional, timestamp for the episode)

    Note: Episodes are processed sequentially in the background.
    The endpoint returns immediately with a 202 Accepted status.
    Returns a task_id that can be used to check the status at /task/{task_id}
    """
    try:
        episode_type = EpisodeType.from_str(data.episode_type.lower())
    except KeyError:
        valid_types = [e.name for e in EpisodeType]
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Invalid episode_type. Must be one of: {', '.join(valid_types)}"
        )

    # Create task tracking entry
    task_store[data.task_id] = {
        "task_id": data.task_id,
        "episode_id": None,
        "status": TaskStatus.PENDING,
        "created_at": datetime.now(),
        "completed_at": None,
        "message": None,
        "error": None
    }

    # Add the episode processing to background tasks
    background_tasks.add_task(
        process_episode_addition,
        task_id=data.task_id,
        episode_type=episode_type,
        description=data.description,
        content=data.content,
        group_id=data.group_id,
        reference_time=data.reference_time or datetime.now()
    )

    return {
        "message": "Episode queued for processing",
        "task_id": data.task_id,
        "status": "accepted"
    }


@app.get("/task/{task_id}", response_model=TaskStatusResponse)
async def get_task_status(task_id: str):
    """Endpoint to check the status of a background task

    Returns:
    - task_id: The unique task identifier
    - status: Current status (pending, processing, completed, failed)
    - message: Success message if completed
    - error: Error message if failed
    - created_at: When the task was created
    - completed_at: When the task completed (if applicable)
    """
    if task_id not in task_store:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Task {task_id} not found"
        )

    return TaskStatusResponse(**task_store[task_id])


@app.get("/episode/{episode_id}", response_model=EpisodeResponse)
async def get_episode(episode_id: str):
    """Endpoint to retrieve a specific episode by its ID"""
    try:
        from graphiti_core.nodes import EpisodicNode
        episode = await EpisodicNode.get_by_uuid(graphiti.driver, episode_id)
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


@app.delete("/delete_data")
async def delete_data(episode_id: str):
    """Endpoint to delete data from the graph database"""
    try:
        await graphiti.remove_episode(episode_id)
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


@app.delete("/clear")
async def clear():
    await clear_data(graphiti.driver)
    await graphiti.build_indices_and_constraints()

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
