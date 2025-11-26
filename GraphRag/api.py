import logging
import os
from contextlib import asynccontextmanager
from datetime import datetime
from enum import Enum
from typing import Optional, List, Dict, Any
from uuid import UUID

import cognee
import uvicorn
from cognee.modules.search.types import SearchType
from fastapi import FastAPI, HTTPException, BackgroundTasks
from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from pydantic import BaseModel
from starlette import status

# OpenTelemetry setup
otlp_exporter = OTLPSpanExporter()
processor = BatchSpanProcessor(otlp_exporter)
tracer_provider = TracerProvider()
tracer_provider.add_span_processor(processor)
trace.set_tracer_provider(tracer_provider)

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)
tracer = trace.get_tracer(__name__)


app = FastAPI(
    title="GraphRAG API (Cognee)",
    description="API for GraphRAG operations using Cognee framework with SQLite, Kuzu, and LanceDB",
    version="2.0.0",
)
FastAPIInstrumentor.instrument_app(app)

class AddDataRequest(BaseModel):
    content: str
    adventure_id: str


class AddDataRequestBatch(BaseModel):
    content: List[str]
    adventure_id: str

class SearchRequest(BaseModel):
    adventure_id: str
    query: str


class SearchResult(BaseModel):
    content: str


class VisualizeRequest(BaseModel):
    path: str

@app.post("/add", status_code=status.HTTP_200_OK)
async def add_data(data: AddDataRequest):

    await cognee.add(data.content, dataset_name=data.adventure_id)
    result = await cognee.cognify(datasets=[data.adventure_id])
    await cognee.memify(dataset=data.adventure_id)
    return result


@app.post("/add-batch", status_code=status.HTTP_200_OK)
async def add_data(data: AddDataRequestBatch):

    for content in data.content:
        await cognee.add(content, dataset_name=data.adventure_id)

    result = await cognee.cognify(datasets=[data.adventure_id])
    await cognee.memify(dataset=data.adventure_id)
    return result


@app.post("/search")
async def search(request: SearchRequest):

    try:

        search_results = await cognee.search(datasets=[request.adventure_id], query_type=SearchType.GRAPH_COMPLETION_CONTEXT_EXTENSION, query_text=request.query)

        return search_results

    except Exception as e:
        logger.error(f"{type(e).__name__}: Error during search: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Search failed: {str(e)}"
        )


@app.delete("/clear")
async def clear():
    try:
        await cognee.prune.prune_data()
    except Exception as e:
        logger.error(f"{type(e).__name__}: Error clearing data: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to clear data: {str(e)}"
        )


@app.delete("/delete/node/{dataset_id}/{data_id}")
async def clear_adventure(dataset_id: UUID, data_id: UUID):
    try:
        await cognee.delete(data_id=data_id, dataset_id=dataset_id)
    except Exception as e:
        logger.error(f"{type(e).__name__}: Error clearing adventure: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to clear adventure: {str(e)}"
        )

@app.delete("/delete/{adventure_id}")
async def clear_adventure(adventure_id: str):
    try:
        await cognee.datasets.delete_dataset(dataset_id=adventure_id)
    except Exception as e:
        logger.error(f"{type(e).__name__}: Error clearing adventure: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to clear adventure: {str(e)}"
        )


@app.get("/visualization")
async def visualization(request: VisualizeRequest):
    """Get visualization data for the knowledge graph"""
    try:
        await cognee.visualize_graph(request.path)
    except Exception as e:
        logger.error(f"Error generating visualization: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to generate visualization: {str(e)}"
        )

@app.get("/")
async def root():
    """Root endpoint to verify API is running"""
    return {"message": "GraphRAG API (Cognee) is running"}


@app.get("/health")
async def health():
    """Health check endpoint"""
    return {"status": "healthy"}


@app.get("/info")
async def info():
    """Get information about the cognee configuration"""
    try:
        return {
            "api_version": "2.0.0",
            "framework": "cognee",
            "databases": {
                "vector": "LanceDB",
                "graph": "Kuzu",
                "metadata": "SQLite"
            },
            "data_directory": os.environ.get('COGNEE_DATA_DIR', '.cognee_system')
        }
    except Exception as e:
        logger.error(f"Error getting info: {str(e)}")
        return {
            "api_version": "2.0.0",
            "framework": "cognee",
            "error": str(e)
        }


if __name__ == '__main__':
    port = int(os.environ.get('PORT', 8112))
    host = os.environ.get('HOST', '127.0.0.1')

    uvicorn.run(
        app,
        host=host,
        port=port,
        reload=bool(os.environ.get('DEBUG', False))
    )
