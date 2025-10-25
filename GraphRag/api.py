import logging
import os
import uvicorn
from typing import List, Optional, Dict
from uuid import UUID

import cognee
from dotenv import set_key, dotenv_values
from cognee.modules.ontology.ontology_config import Config
from cognee.modules.ontology.rdf_xml.RDFLibOntologyResolver import RDFLibOntologyResolver
from cognee.modules.search.types import SearchType
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="GraphRAG Search API")


class SearchRequest(BaseModel):
    query: str = Field(..., description="The search query text")
    query_type: str = Field(..., description="The type of search query (e.g., GRAPH_COMPLETION_CONTEXT_EXTENSION)")
    system_prompt: str = Field(None, description="System prompt for the search")
    top_k: int = Field(..., description="Number of top results to return", gt=0)
    dataset_ids: List[UUID] = Field(None, description="Optional list of dataset IDs to restrict the search")


class SearchResponse(BaseModel):
    results: List[str]


class BuildIndexRequest(BaseModel):
    text: str = Field(..., description="The text content to process")
    dataset_id: Optional[UUID] = Field(None, description="Dataset ID")
    dataset_name: str = Field(..., description="Name of the dataset")
    ontology: Optional[str] = Field(None, description="Optional ontology string in OWL/RDF format")


class BuildIndexResponse(BaseModel):
    dataset_id: UUID
    data_id: UUID


class PipelineStatusResponse(BaseModel):
    dataset_id: UUID
    status: str


class DeleteRequest(BaseModel):
    dataset_id: UUID = Field(..., description="ID of the dataset")
    data_id: UUID = Field(..., description="ID of the data to delete")


class VisualizeGraphRequest(BaseModel):
    output_path: Optional[str] = Field(None, description="Optional path to save the visualization HTML file")


class SetEnvVarRequest(BaseModel):
    key: str = Field(..., description="Environment variable key")
    value: str = Field(..., description="Environment variable value")


class SetEnvVarsRequest(BaseModel):
    variables: Dict[str, str] = Field(..., description="Dictionary of environment variables to set")


class EnvVarResponse(BaseModel):
    key: str
    value: str


@app.post("/build_index", response_model=BuildIndexResponse)
async def build_index(request: BuildIndexRequest):
    """
    Build an index from text data for search and retrieval.

    Args:
        request: BuildIndexRequest object containing text, dataset_id, dataset_name, and optional ontology

    Returns:
        BuildIndexResponse with the dataset_id and pipeline_run_id
    """
    temp_ontology_file = None
    try:
        logger.info(f"Build index request - Dataset Name: {request.dataset_name}, Dataset ID: {request.dataset_id}, Text length: {len(request.text)}")
        result_dataset_id = await cognee.add(
            request.text,
            dataset_name=request.dataset_name,
            dataset_id=request.dataset_id
        )
        # status='PipelineRunCompleted' pipeline_run_id=UUID('c10f06d5-d24e-5ef7-8b7f-0ca50d754a7d') dataset_id=UUID('9a0c6d09-d93c-52ea-af41-11156a155d25') dataset_name='adventure' payload=None data_ingestion_info=[{'run_info': PipelineRunAlreadyCompleted(status='PipelineRunAlreadyCompleted', pipeline_run_id=UUID('c10f06d5-d24e-5ef7-8b7f-0ca50d754a7d'), dataset_id=UUID('9a0c6d09-d93c-52ea-af41-11156a155d25'), dataset_name='adventure', payload=None, data_ingestion_info=None), 'data_id': UUID('d129295b-c813-5a70-9432-9c6291d16f3e')}], Cognify run info: {UUID('9a0c6d09-d93c-52ea-af41-11156a155d25'): PipelineRunStarted(status='PipelineRunStarted', pipeline_run_id=UUID('c32b6f23-0221-54fc-ba53-2a884bf53847'), dataset_id=UUID('9a0c6d09-d93c-52ea-af41-11156a155d25'), dataset_name='adventure', payload=[], data_ingestion_info=None)}
        logger.info(f"Dataset ID: {result_dataset_id}")

        ontology_path = None
        if request.ontology:
            temp_dir = os.path.abspath("temp")
            os.makedirs(temp_dir, exist_ok=True)

            temp_ontology_file = os.path.join(temp_dir, f"ontology_{result_dataset_id}.owl")
            with open(temp_ontology_file, 'w', encoding='utf-8') as f:
                f.write(request.ontology)
            logger.info(f"Ontology written to temp file: {temp_ontology_file}")
            ontology_path = temp_ontology_file

        config: Config = {
            "ontology_config": {
                "ontology_resolver": RDFLibOntologyResolver(ontology_file=ontology_path)
            }
        }

        # Example run_info:
        # run info {UUID('faf31318-8eb6-5d35-881a-5f2553456d45'): PipelineRunStarted(status='PipelineRunStarted', pipeline_run_id=UUID('8551eec4-3aae-5eb4-a3f3-4f1bbfbe65ea'), dataset_id=UUID('faf31318-8eb6-5d35-881a-5f2553456d45'), dataset_name='ariel_description', payload=[], data_ingestion_info=None)}
        run_info = await cognee.cognify(datasets=request.dataset_name, run_in_background=True, config=config)
        logger.info(f"Build index completed successfully for dataset: {request.dataset_name}, Dataset ID: {result_dataset_id}, Cognify run info: {run_info}")

        data_id = result_dataset_id.data_ingestion_info[0]['data_id']

        return BuildIndexResponse(dataset_id=result_dataset_id.dataset_id, data_id=data_id)

    except Exception as e:
        if temp_ontology_file and os.path.exists(temp_ontology_file):
            try:
                os.remove(temp_ontology_file)
                logger.info(f"Cleaned up temp ontology file: {temp_ontology_file}")
            except Exception as cleanup_error:
                logger.error(f"Failed to clean up temp file: {cleanup_error}")

        logger.error(f"Build index failed: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Build index failed: {str(e)}")


@app.post("/search", response_model=SearchResponse)
async def search(request: SearchRequest):
    """
    Perform a search using the cognee library with the specified parameters.

    Args:
        request: SearchRequest object containing query, query_type, system_prompt, and top_k

    Returns:
        SearchResponse with list of result strings
    """
    try:
        try:
            search_type = SearchType[request.query_type]
        except KeyError:
            raise HTTPException(
                status_code=400,
                detail=f"Invalid query_type. Must be one of: {[e.name for e in SearchType]}"
            )

        logger.info(f"Search request - Query: {request.query}, Query Type: {request.query_type}, System Prompt: {request.system_prompt}, Top K: {request.top_k}")

        results = await cognee.search(
            query_type=search_type,
            query_text=request.query,
            system_prompt=request.system_prompt,
            top_k=request.top_k
        )

        results_list = []
        if results:
            for result in results:
                if isinstance(result, str):
                    results_list.append(result)
                else:
                    results_list.append(str(result))

        logger.info(f"Search results: {results_list}")

        return SearchResponse(results=results_list)

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Search failed: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Search failed: {str(e)}")


@app.get("/pipeline_status/{dataset_id}", response_model=PipelineStatusResponse)
async def get_pipeline_status(dataset_id: UUID):
    """
    Get the current status of a pipeline for a specific dataset.
    When the pipeline is completed, cleans up any temporary ontology file.

    Args:
        dataset_id: UUID of the dataset to check status for

    Returns:
        PipelineStatusResponse with dataset_id and current status
    """
    try:
        status_dict = await cognee.datasets.get_status([dataset_id])
    except Exception as e:
        logger.error(f"Get pipeline status failed: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Get pipeline status failed: {str(e)}")

    if str(dataset_id) not in status_dict:
        raise HTTPException(status_code=404, detail=f"No pipeline status found for dataset ID: {dataset_id}")

    status = status_dict[str(dataset_id)]
    logger.info("Pipeline status for dataset ID {}: {}".format(dataset_id, status))
    if status in ["PipelineRunCompleted", "completed", "COMPLETED"]:
        temp_ontology_file = os.path.join(os.path.abspath("temp"), f"ontology_{dataset_id}.owl")
        if os.path.exists(temp_ontology_file):
            try:
                os.remove(temp_ontology_file)
                logger.info(f"Cleaned up temp ontology file: {temp_ontology_file}")
            except Exception as cleanup_error:
                logger.error(f"Failed to clean up temp ontology file: {cleanup_error}")

    return PipelineStatusResponse(dataset_id=dataset_id, status=status)


@app.delete("/delete")
async def delete_data(request: DeleteRequest):
    """
    Delete specific data from a dataset.

    Args:
        request: DeleteRequest object containing dataset_name and data_id

    Returns:
        DeleteResponse with success status and message
    """
    try:
        await cognee.delete(dataset_id=request.dataset_id, data_id=request.data_id)

    except Exception as e:
        logger.error(f"Delete failed: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Delete failed: {str(e)}")


@app.post("/visualize_graph")
async def visualize_graph(request: VisualizeGraphRequest):
    """
    Generate a visualization of the knowledge graph.

    Args:
        request: VisualizeGraphRequest object with optional output_path

    Returns:
        HTML content of the graph visualization or path to saved file
    """
    try:
        if request.output_path:
            output_path = os.path.abspath(request.output_path)
            await cognee.visualize_graph(output_path)
            logger.info(f"Graph visualization saved to: {output_path}")
            return {"message": "Graph visualization created successfully", "path": output_path}
        else:
            temp_dir = os.path.abspath("temp")
            os.makedirs(temp_dir, exist_ok=True)
            temp_file = os.path.join(temp_dir, "temp_graph_visualization.html")

            await cognee.visualize_graph(temp_file)

            with open(temp_file, 'r', encoding='utf-8') as f:
                html_content = f.read()

            try:
                os.remove(temp_file)
            except Exception as cleanup_error:
                logger.warning(f"Failed to clean up temp visualization file: {cleanup_error}")

            logger.info("Graph visualization generated and returned as HTML")
            from fastapi.responses import HTMLResponse
            return HTMLResponse(content=html_content)

    except Exception as e:
        logger.error(f"Visualize graph failed: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Visualize graph failed: {str(e)}")


@app.post("/env/set", response_model=EnvVarResponse)
async def set_env_variable(request: SetEnvVarRequest):
    """
    Set a single environment variable in the .env file.

    Args:
        request: SetEnvVarRequest object containing key and value

    Returns:
        EnvVarResponse with the key, value, and success message
    """
    try:
        env_file_path = os.path.join(os.path.dirname(__file__), ".env")

        set_key(env_file_path, request.key, request.value)

        os.environ[request.key] = request.value

        logger.info(f"Environment variable set: {request.key}")

        return EnvVarResponse(
            key=request.key,
            value=request.value
        )

    except Exception as e:
        logger.error(f"Failed to set environment variable: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Failed to set environment variable: {str(e)}")


@app.post("/env/set_multiple")
async def set_env_variables(request: SetEnvVarsRequest):
    """
    Set multiple environment variables in the .env file at once.

    Args:
        request: SetEnvVarsRequest object containing a dictionary of key-value pairs

    Returns:
        Dictionary with success status and list of updated variables
    """
    try:
        env_file_path = os.path.join(os.path.dirname(__file__), ".env")
        updated_vars = []

        for key, value in request.variables.items():
            set_key(env_file_path, key, value)

            os.environ[key] = value

            updated_vars.append({"key": key, "value": value})

        logger.info(f"Environment variables set: {list(request.variables.keys())}")

        return {
            "message": f"Successfully set {len(updated_vars)} environment variable(s)",
            "updated_variables": updated_vars
        }

    except Exception as e:
        logger.error(f"Failed to set environment variables: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Failed to set environment variables: {str(e)}")


@app.get("/env/get/{key}")
async def get_env_variable(key: str):
    """
    Get the value of a specific environment variable.

    Args:
        key: The environment variable key to retrieve

    Returns:
        Dictionary with the key and its value
    """
    try:
        env_file_path = os.path.join(os.path.dirname(__file__), ".env")

        env_vars = dotenv_values(env_file_path)

        if key in env_vars:
            return {"key": key, "value": env_vars[key]}
        else:
            raise HTTPException(status_code=404, detail=f"Environment variable '{key}' not found")

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Failed to get environment variable: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Failed to get environment variable: {str(e)}")


@app.get("/env/list")
async def list_env_variables():
    """
    List all environment variables from the .env file.

    Returns:
        Dictionary containing all environment variables
    """
    try:
        env_file_path = os.path.join(os.path.dirname(__file__), ".env")

        env_vars = dotenv_values(env_file_path)

        return {
            "count": len(env_vars),
            "variables": env_vars
        }

    except Exception as e:
        logger.error(f"Failed to list environment variables: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Failed to list environment variables: {str(e)}")


@app.delete("/env/delete/{key}")
async def delete_env_variable(key: str):
    """
    Delete an environment variable from the .env file.

    Args:
        key: The environment variable key to delete

    Returns:
        Dictionary with success message
    """
    try:
        env_file_path = os.path.join(os.path.dirname(__file__), ".env")

        env_vars = dotenv_values(env_file_path)

        if key not in env_vars:
            raise HTTPException(status_code=404, detail=f"Environment variable '{key}' not found")

        with open(env_file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()

        with open(env_file_path, 'w', encoding='utf-8') as f:
            for line in lines:
                if not line.strip().startswith(f"{key}=") and not line.strip().startswith(f'{key}='):
                    f.write(line)

        if key in os.environ:
            del os.environ[key]

        logger.info(f"Environment variable deleted: {key}")

        return {"message": f"Environment variable '{key}' has been deleted successfully"}

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Failed to delete environment variable: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=f"Failed to delete environment variable: {str(e)}")


@app.get("/")
async def root():
    """Root endpoint to verify API is running"""
    return {"message": "GraphRAG Search API is running", "version": "1.0.0"}


@app.get("/health")
async def health():
    """Health check endpoint"""
    return {"status": "healthy"}


if __name__ == "__main__":
    port = int(os.getenv("PORT", 8000))
    postgres = os.getenv("ConnectionStrings__graphrag")

    neo4j_uri = os.getenv("NEO4J_URI")
    neo4j_user = os.getenv("NEO4J_USER")
    neo4j_password = os.getenv("NEO4J_PASSWORD")

    env_file_path = os.path.join(os.path.dirname(__file__), ".env")
    
    if neo4j_uri or neo4j_user or neo4j_password:
        try:
            if neo4j_uri:
                set_key(env_file_path, "GRAPH_DATABASE_URL", neo4j_uri)
                logger.info("Updated GRAPH_DATABASE_URL in .env file: {}".format(neo4j_uri))
            
            if neo4j_user:
                set_key(env_file_path, "GRAPH_DATABASE_USERNAME", neo4j_user)
                logger.info("Updated GRAPH_DATABASE_USERNAME in .env file: {}".format(neo4j_user))
            
            if neo4j_password:
                set_key(env_file_path, "GRAPH_DATABASE_PASSWORD", neo4j_password)
                logger.info("Updated GRAPH_DATABASE_PASSWORD in .env file")
                
        except Exception as e:
            logger.error(f"Failed to update .env file with Neo4j credentials: {str(e)}")
            raise e

    if postgres:
        try:
            # Format: Host=localhost;Port=65066;Username=postgres;Password=MBt~s{fbNDwxqS3xkzX)Fu;Database=dbname
            conn_params = {}
            for param in postgres.split(';'):
                if '=' in param:
                    key, value = param.split('=', 1)
                    conn_params[key.strip().lower()] = value.strip()

            if 'host' in conn_params:
                set_key(env_file_path, "DB_HOST", conn_params['host'])
                logger.info("Updated DB_HOST in .env file")

            if 'port' in conn_params:
                set_key(env_file_path, "DB_PORT", conn_params['port'])
                logger.info("Updated DB_PORT in .env file:")

            if 'username' in conn_params:
                set_key(env_file_path, "DB_USERNAME", conn_params['username'])
                logger.info("Updated DB_USERNAME in .env file")

            if 'password' in conn_params:
                set_key(env_file_path, "DB_PASSWORD", conn_params['password'])
                logger.info("Updated DB_PASSWORD in .env file")

            if 'database' in conn_params:
                set_key(env_file_path, "DB_NAME", conn_params['database'])
                logger.info("Updated DB_NAME in .env file")

        except Exception as e:
            logger.error(f"Failed to update .env file with Postgres credentials: {str(e)}")
            raise e
    
    uvicorn.run(app, host="127.0.0.1", port=port)

