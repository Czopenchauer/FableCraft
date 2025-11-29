import json
import logging
import os
from typing import Any, List, Optional
from uuid import UUID

import cognee
import uvicorn
from cognee.exceptions import CogneeApiError
from cognee.modules.search.types import SearchType
from fastapi import FastAPI, HTTPException
from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from pydantic import BaseModel
from starlette import status

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
    content: List[str]
    adventure_id: str


class SearchRequest(BaseModel):
    adventure_id: str
    query: str
    search_type: SearchType


class PipelineRunInfo(BaseModel):
    status: str
    pipeline_run_id: UUID
    dataset_id: UUID
    dataset_name: str
    payload: Optional[Any] = None
    data_ingestion_info: Optional[Any] = None


class DataIngestionInfo(BaseModel):
    run_info: PipelineRunInfo
    data_id: UUID


class AddDataResponse(BaseModel):
    status: str
    pipeline_run_id: UUID
    dataset_id: UUID
    dataset_name: str
    payload: Optional[Any] = None
    data_ingestion_info: Optional[List[DataIngestionInfo]] = None



class SearchResponse(BaseModel):
    results: List[str]


class VisualizeRequest(BaseModel):
    path: str


class UpdateDataRequest(BaseModel):
    adventure_id: str
    data_id: UUID
    content: str


@app.post("/add", status_code=status.HTTP_200_OK)
async def add_data(data: AddDataRequest):
    """Add data to a dataset without processing"""
    try:
        logger.info("Adding data to dataset %s", data.adventure_id)
        result = await cognee.add(data.content, dataset_name=data.adventure_id)
        logger.info("Add completed %s", result)

        datasets = await cognee.datasets.list_datasets()
        dataset_summaries = [{"id": getattr(d, "id", None), "name": getattr(d, "name", None)} for d in datasets]
        logger.info("Datasets after processing %s: %s", data.adventure_id, dataset_summaries)

        dataset = next((d for d in datasets if d.name == data.adventure_id), None)
        if not dataset:
            raise ValueError(f"Dataset '{data.adventure_id}' not found after processing")

        dataset_data = await cognee.datasets.list_data(dataset.id)

        data_ids = set()
        result_obj = getattr(result, "result", None) if hasattr(result, "result") else result
        data_ingestion_info = getattr(result_obj, "data_ingestion_info", None)

        if data_ingestion_info:
            for info in data_ingestion_info:
                # info is a dict, not an object
                data_id = info.get("data_id") if isinstance(info, dict) else getattr(info, "data_id", None)
                if data_id:
                    data_ids.add(str(data_id))

        file_name_to_id = {}
        for item in dataset_data:
            item_id = str(getattr(item, "id", ""))
            if item_id in data_ids:
                file_name = getattr(item, "name", "")
                file_name_to_id[file_name] = item_id

        logger.info(file_name_to_id)
        return file_name_to_id

    except Exception as e:
        logger.error(f"{type(e).__name__}: Error during add data: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Add data failed: {str(e)}"
        )


@app.post("/cognify/{adventure_id}", status_code=status.HTTP_200_OK)
async def cognify_dataset(adventure_id: str):
    """Run cognify processing on a dataset"""
    try:
        logger.info("Running cognify for dataset %s", adventure_id)
        result = await cognee.cognify(datasets=[adventure_id])
        logger.info("Cognify result for %s: %s", adventure_id, result)
        await cognee.visualize_graph(f"./visualization/{adventure_id}_cognify_graph_visualization.html")

    except Exception as e:
        logger.error(f"{type(e).__name__}: Error during cognify: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Cognify failed: {str(e)}"
        )


@app.post("/memify/{adventure_id}", status_code=status.HTTP_200_OK)
async def memify_dataset(adventure_id: str):
    """Run memify processing on a dataset"""
    try:
        logger.info("Running memify for dataset %s", adventure_id)
        mem_result = await cognee.memify(dataset=adventure_id)
        logger.info("Memify result for %s: %s", adventure_id, mem_result)
        await cognee.visualize_graph(f"./visualization/{adventure_id}_memify_graph_visualization.html")

    except Exception as e:
        logger.error(f"{type(e).__name__}: Error during memify: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Memify failed: {str(e)}"
        )


@app.get("/datasets/{adventure_id}")
async def get_datasets(adventure_id: str):

    datasets = await cognee.datasets.list_datasets()
    dataset_summaries = [{"id": getattr(d, "id", None), "name": getattr(d, "name", None)} for d in datasets]
    logger.info("Datasets after processing %s: %s", adventure_id, dataset_summaries)

    dataset = next((d for d in datasets if d.name == adventure_id), None)
    if not dataset:
        raise ValueError(f"Dataset '{adventure_id}' not found after processing")

    dataset_data = await cognee.datasets.list_data(dataset.id)
    logger.info("Dataset data for %s: %s", adventure_id, dataset_data)
    return dataset_data


@app.post("/search")
async def search(request: SearchRequest, response_model=SearchResponse):

    try:
        search_results = await cognee.search(datasets=[request.adventure_id], query_type=request.search_type, query_text=request.query)

        results = [
            text
            for result in search_results
            if result.get("search_result")
            for text in result["search_result"]
        ]

        return SearchResponse(results=results)

    except Exception as e:
        logger.error(f"{type(e).__name__}: Error during search: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Search failed: {str(e)}"
        )


@app.delete("/nuke")
async def nuke():
    try:
        await cognee.prune.prune_data()
        await cognee.prune.prune_system(metadata=True)
    except Exception as e:
        logger.error(f"{type(e).__name__}: Error clearing data: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to clear data: {str(e)}"
        )


@app.delete("/delete/node/{dataset_name}/{data_id}")
async def delete_node(dataset_name: str, data_id: UUID):
    try:
        datasets = await cognee.datasets.list_datasets()
        dataset = next((d for d in datasets if d.name == dataset_name), None)

        if not dataset:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Dataset with name '{dataset_name}' not found"
            )

        dataset_id = UUID(dataset.id)
        await cognee.delete(data_id=data_id, dataset_id=dataset_id)

        return {"message": f"Successfully deleted node {data_id} from dataset {dataset_name}"}
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"{type(e).__name__}: Error clearing adventure: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to clear adventure: {str(e)}"
        )


@app.put("/update")
async def update_node(request: UpdateDataRequest):
    try:
        datasets = await cognee.datasets.list_datasets()
        dataset = next((d for d in datasets if d.name == request.adventure_id), None)

        if not dataset:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Dataset with name '{request.adventure_id}' not found"
            )

        await cognee.update(data_id=request.data_id, dataset_id=dataset.id, data=request.content)

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"{type(e).__name__}: Error clearing adventure: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to clear adventure: {str(e)}"
        )


@app.delete("/delete/{adventure_id}")
async def clear_adventure(adventure_id: str):
    try:
        datasets = await cognee.datasets.list_datasets()
        dataset = next((d for d in datasets if d.name == adventure_id), None)

        if not dataset:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Dataset with name '{adventure_id}' not found"
            )

        dataset_id = dataset.id
        await cognee.datasets.delete_dataset(dataset_id=dataset_id)
    except Exception as e:
        logger.error(f"{type(e).__name__}: Error clearing adventure: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to clear adventure: {str(e)}"
        )


@app.post("/visualization")
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
