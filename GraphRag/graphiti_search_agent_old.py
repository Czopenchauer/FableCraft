import logging
import os
from typing import List, Dict, Any, Optional

from langchain_core.messages import HumanMessage
from langchain_openai import ChatOpenAI
from langgraph.graph import StateGraph, END
from pydantic import BaseModel, Field, SecretStr

from graphiti_core import Graphiti
from graphiti_core.search.search import SearchConfig
from graphiti_core.search.search_config import NodeSearchConfig, EdgeReranker, EdgeSearchMethod, EdgeSearchConfig, \
    NodeSearchMethod, NodeReranker

logger = logging.getLogger(__name__)

# ==================== RELEVANCE THRESHOLDS ====================
MIN_RELEVANCE_THRESHOLD = 0.5  # For nodes and edges
MIN_COMMUNITY_THRESHOLD = 0.3  # Lower threshold for communities
MAX_EDGE_FACTS_FOR_EXPANSION = 5  # Limit for LLM processing


class GraphSearchState(BaseModel):
    """State for comprehensive graph exploration"""
    # User input
    user_query: str
    group_id: str
    # Search control
    search_depth: int = 3
    current_depth: int = 0
    max_search_results_per_round: int = 15

    # Retrieved data
    edges: List[Dict[str, Any]] = Field(default_factory=list)
    nodes: List[Dict[str, Any]] = Field(default_factory=list)
    communities: List[Dict[str, Any]] = Field(default_factory=list)

    # Tracking
    explored_entities: List[str] = Field(default_factory=list)
    search_queries: List[str] = Field(default_factory=list)

    # Synthesis
    intermediate_syntheses: List[str] = Field(default_factory=list)
    final_answer: str = ""

    # Reflection
    reflection_feedback: Optional[str] = None
    needs_regeneration: bool = False
    regeneration_count: int = 0


# ==================== CONFIGURATION ====================

class GraphAgentConfig:
    """Configuration for the graph search agent"""

    def __init__(
            self,
            graphiti_client: Graphiti,
            search_depth: int = 3,
            max_results_per_round: int = 15,
            enable_reflection: bool = True,
            max_regenerations: int = 2
    ):
        api_key = os.environ.get("LLM_API_KEY")
        model = os.environ.get("LLM_MODEL")
        base_url = os.environ.get("LLM_ENDPOINT")
        self.graphiti = graphiti_client
        self.llm = ChatOpenAI(
            base_url=base_url,
            model=model,
            temperature=0.3,
            api_key=SecretStr(api_key))
        self.search_depth = search_depth
        self.max_results_per_round = max_results_per_round
        self.enable_reflection = enable_reflection
        self.max_regenerations = max_regenerations


# ==================== NODE IMPLEMENTATIONS ====================

class GraphQueryNode:
    """
    Generates search queries for the knowledge graph.
    Adapts queries based on previous results and depth.
    """

    def __init__(self, config: GraphAgentConfig):
        self.config = config
        self.llm = config.llm

    async def __call__(self, state: GraphSearchState) -> Dict[str, Any]:
        """Generate search queries based on current state"""

        current_depth = state.current_depth
        user_query = state.user_query

        # Generate queries based on depth
        if current_depth == 0:
            # Initial broad search
            queries = [user_query]
        else:
            # Generate expansion queries based on previous findings
            queries = await self._generate_expansion_queries(state)

        return {
            "search_queries": queries,
            "current_depth": current_depth
        }

    async def _extract_key_concepts_from_facts(self, edges: List[Dict[str, Any]]) -> List[str]:
        """Extract key concepts from edge facts using LLM"""
        if not edges:
            return []

        # Prepare facts for LLM
        facts_text = "\n".join([
            f"{i + 1}. {edge.get('fact', '')}"
            for i, edge in enumerate(edges[:MAX_EDGE_FACTS_FOR_EXPANSION])
            if edge.get('fact')
        ])

        if not facts_text:
            return []

        prompt = f"""Extract 2-3 key concepts from each fact below. Include both:
                    1. Named entities (characters, locations, items)
                    2. Abstract concepts (themes, actions like "rescue", "betrayal", "conflict")

                    FACTS:
                    {facts_text}
                    
                    Return ONLY a comma-separated list of concepts, no explanations.
                    Example: Ariel, village, rescue, heroism, bandits"""

        try:
            response = await self.llm.ainvoke([HumanMessage(content=prompt)])
            concepts_text = response.content.strip()
            # Parse comma-separated concepts
            concepts = [c.strip() for c in concepts_text.split(',') if c.strip()]
            return concepts[:15]  # Limit total concepts
        except Exception as e:
            logger.error(f"Error extracting concepts from facts: {e}")
            return []

    async def _generate_expansion_queries(self, state: GraphSearchState) -> List[str]:
        """Generate queries to explore related entities"""

        # Extract entity names from previously found nodes
        entity_names = []
        for node in state.nodes:
            if "name" in node:
                entity_names.append(node["name"])

        # Extract key concepts from edges (relationship types)
        key_concepts = set()
        for edge in state.edges:
            if "name" in edge:
                key_concepts.add(edge["name"])

        # Extract key concepts from edge facts using LLM
        recent_edges = sorted(
            state.edges,
            key=lambda e: e.get('depth_discovered', 0),
            reverse=True
        )[:MAX_EDGE_FACTS_FOR_EXPANSION]

        fact_concepts = await self._extract_key_concepts_from_facts(recent_edges)

        # Build expansion queries
        base_query = state.user_query
        expansion_queries = [base_query]

        # Add entity-specific queries (limit to top 3 most relevant)
        for entity in entity_names[:3]:
            expansion_queries.append(f"{base_query} {entity}")

        # Add relationship-type queries
        for concept in list(key_concepts)[:2]:
            expansion_queries.append(f"{base_query} {concept}")

        # Add fact-extracted concept queries
        for concept in fact_concepts[:3]:
            expansion_queries.append(f"{base_query} {concept}")

        return expansion_queries


class GraphToolsNode:
    """
    Executes searches against Graphiti knowledge graph.
    Uses hybrid search with cross-encoder reranking for best results.
    """

    def __init__(self, config: GraphAgentConfig):
        self.config = config
        self.graphiti = config.graphiti

    async def __call__(self, state: GraphSearchState) -> Dict[str, Any]:
        """Execute graph searches and aggregate results"""

        queries = state.search_queries
        current_depth = state.current_depth

        # Start with existing results from state (accumulation, not replacement)
        all_edges = list(state.edges)
        all_nodes = list(state.nodes)
        all_communities = list(state.communities)
        explored_entities = list(state.explored_entities)

        # Track UUIDs for deduplication (initialize from existing state)
        existing_edge_uuids = {edge['uuid'] for edge in state.edges}
        existing_node_uuids = {node['uuid'] for node in state.nodes}
        existing_community_uuids = {comm['uuid'] for comm in state.communities}

        for query in queries:  # Process all queries generated

            # Execute hybrid search with cross-encoder reranking
            config = SearchConfig(
                edge_config=EdgeSearchConfig(
                    search_methods=[
                        EdgeSearchMethod.bm25,
                        EdgeSearchMethod.cosine_similarity,
                    ],
                    reranker=EdgeReranker.episode_mentions,
                ),
                node_config=NodeSearchConfig(
                    search_methods=[
                        NodeSearchMethod.cosine_similarity,
                        NodeSearchMethod.bfs,
                    ],
                    reranker=NodeReranker.cross_encoder,
                ),
                limit=10,
            )

            results = await self.graphiti.search_(
                group_ids=[state.group_id],
                query=query,
                config=config
            )

            # Filter and aggregate edges by relevance score
            if results.edges and results.edge_reranker_scores:
                for edge, score in zip(results.edges, results.edge_reranker_scores):
                    if score >= MIN_RELEVANCE_THRESHOLD and edge.uuid not in existing_edge_uuids:
                        all_edges.append({
                            "uuid": edge.uuid,
                            "name": edge.name,
                            "fact": edge.fact,
                            "source_node_uuid": edge.source_node_uuid,
                            "target_node_uuid": edge.target_node_uuid,
                            "episodes": edge.episodes,
                            "valid_at": edge.valid_at,
                            "invalid_at": edge.invalid_at,
                            "depth_discovered": current_depth,
                            "relevance_score": score
                        })
                        existing_edge_uuids.add(edge.uuid)

            # Filter and aggregate nodes by relevance score
            if results.nodes and results.node_reranker_scores:
                for node, score in zip(results.nodes, results.node_reranker_scores):
                    if score >= MIN_RELEVANCE_THRESHOLD and node.uuid not in existing_node_uuids:
                        all_nodes.append({
                            "uuid": node.uuid,
                            "name": node.name,
                            "summary": node.summary,
                            "depth_discovered": current_depth,
                            "relevance_score": score
                        })
                        existing_node_uuids.add(node.uuid)
                        explored_entities.append(node.uuid)

            # Filter and aggregate communities by lower threshold
            if results.communities and results.community_reranker_scores:
                for community, score in zip(results.communities, results.community_reranker_scores):
                    if score >= MIN_COMMUNITY_THRESHOLD and community.uuid not in existing_community_uuids:
                        all_communities.append({
                            "uuid": community.uuid,
                            "name": community.name,
                            "summary": community.summary,
                            "depth_discovered": current_depth,
                            "relevance_score": score
                        })
                        existing_community_uuids.add(community.uuid)

        # Perform expansions if needed (explore neighbors of found entities)
        new_nodes_this_round = [n for n in all_nodes if n.get('depth_discovered') == current_depth]
        if current_depth > 0 and new_nodes_this_round:
            import asyncio

            # Get high-scoring edges from this round for expansion
            new_edges_this_round = [e for e in all_edges if e.get('depth_discovered') == current_depth]
            high_scoring_edges = [e for e in new_edges_this_round if e.get('relevance_score', 0) >= MIN_RELEVANCE_THRESHOLD]

            # Use recent nodes from previous iterations for BFS
            recent_nodes = [n for n in all_nodes if n.get('depth_discovered') == current_depth - 1][-5:]

            # Run both expansions concurrently
            bfs_task = self._expand_via_bfs(recent_nodes, state.user_query)
            edge_task = self._expand_via_edges(high_scoring_edges[:3], state.group_id)

            expanded_results = await asyncio.gather(bfs_task, edge_task, return_exceptions=True)

            # Process BFS expansion results
            if not isinstance(expanded_results[0], Exception):
                for edge in expanded_results[0]:
                    if edge['uuid'] not in existing_edge_uuids:
                        all_edges.append(edge)
                        existing_edge_uuids.add(edge['uuid'])
            else:
                logger.error(f"BFS expansion error: {expanded_results[0]}")

            # Process edge-based expansion results
            if not isinstance(expanded_results[1], Exception):
                exp_nodes, exp_edges = expanded_results[1]
                for node in exp_nodes:
                    if node['uuid'] not in existing_node_uuids:
                        all_nodes.append(node)
                        existing_node_uuids.add(node['uuid'])
                        explored_entities.append(node['uuid'])
                for edge in exp_edges:
                    if edge['uuid'] not in existing_edge_uuids:
                        all_edges.append(edge)
                        existing_edge_uuids.add(edge['uuid'])
            else:
                logger.error(f"Edge expansion error: {expanded_results[1]}")

        return {
            "edges": all_edges,
            "nodes": all_nodes,
            "communities": all_communities,
            "explored_entities": explored_entities,
            "current_depth": current_depth + 1
        }

    async def _expand_via_bfs(
            self,
            seed_nodes: List[Dict[str, Any]],
            query: str
    ) -> List[Dict[str, Any]]:
        """
        Perform BFS expansion from seed nodes to find connected facts.
        This explores the graph structure to find related information.
        """

        expanded_edges = []

        for node in seed_nodes:
            node_uuid = node.get("uuid")
            if not node_uuid:
                continue

            # Custom Cypher query to find edges connected to this node
            cypher_query = f"""
            MATCH (n:Entity {{uuid: '{node_uuid}'}})-[r]->(m:Entity)
            RETURN r.uuid as uuid, r.name as name, r.fact as fact, 
                   r.source_node_uuid as source_node_uuid,
                   r.target_node_uuid as target_node_uuid,
                   r.episodes as episodes,
                   r.valid_at as valid_at,
                   r.invalid_at as invalid_at
            LIMIT 5
            """

            # Execute via graphiti's driver
            try:
                # Use the driver's execute_query method correctly
                records, summary, keys = await self.graphiti.driver.execute_query(
                    cypher_query,
                    database_="neo4j"
                )

                for record in records:
                    expanded_edges.append({
                        "uuid": record.get("uuid"),
                        "name": record.get("name"),
                        "fact": record.get("fact"),
                        "source_node_uuid": record.get("source_node_uuid"),
                        "target_node_uuid": record.get("target_node_uuid"),
                        "episodes": record.get("episodes"),
                        "valid_at": record.get("valid_at"),
                        "invalid_at": record.get("invalid_at"),
                        "depth_discovered": -1,  # Mark as BFS-discovered
                        "relevance_score": 0.6  # Default score for structural expansion
                    })
            except Exception as e:
                logger.error(f"BFS expansion error for node {node_uuid}: {e}")
                continue

        return expanded_edges

    async def _expand_via_edges(
            self,
            seed_edges: List[Dict[str, Any]],
            group_id: str
    ) -> tuple[List[Dict[str, Any]], List[Dict[str, Any]]]:
        """
        Perform semantic expansion using edge facts as search queries.
        This discovers entities/relationships with similar semantic meaning.
        """

        expanded_nodes = []
        expanded_edges = []

        if not seed_edges:
            return expanded_nodes, expanded_edges

        # Use edge facts as semantic search queries
        for edge in seed_edges:
            fact = edge.get('fact', '')
            if not fact:
                continue

            try:
                # Create search config for semantic expansion
                config = SearchConfig(
                    edge_config=EdgeSearchConfig(
                        search_methods=[
                            EdgeSearchMethod.cosine_similarity,
                        ],
                        reranker=EdgeReranker.episode_mentions,
                    ),
                    node_config=NodeSearchConfig(
                        search_methods=[
                            NodeSearchMethod.cosine_similarity,
                        ],
                        reranker=NodeReranker.cross_encoder,
                    ),
                    limit=5,
                )

                results = await self.graphiti.search_(
                    group_ids=[group_id],
                    query=fact,
                    config=config
                )

                # Filter and collect nodes by relevance
                if results.nodes and results.node_reranker_scores:
                    for node, score in zip(results.nodes, results.node_reranker_scores):
                        if score >= MIN_RELEVANCE_THRESHOLD:
                            expanded_nodes.append({
                                "uuid": node.uuid,
                                "name": node.name,
                                "summary": node.summary,
                                "depth_discovered": -1,  # Mark as edge-expansion discovered
                                "relevance_score": score
                            })

                # Filter and collect edges by relevance
                if results.edges and results.edge_reranker_scores:
                    for result_edge, score in zip(results.edges, results.edge_reranker_scores):
                        if score >= MIN_RELEVANCE_THRESHOLD:
                            expanded_edges.append({
                                "uuid": result_edge.uuid,
                                "name": result_edge.name,
                                "fact": result_edge.fact,
                                "source_node_uuid": result_edge.source_node_uuid,
                                "target_node_uuid": result_edge.target_node_uuid,
                                "episodes": result_edge.episodes,
                                "valid_at": result_edge.valid_at,
                                "invalid_at": result_edge.invalid_at,
                                "depth_discovered": -1,  # Mark as edge-expansion discovered
                                "relevance_score": score
                            })

            except Exception as e:
                logger.error(f"Edge-based expansion error for fact '{fact[:50]}...': {e}")
                continue

        return expanded_nodes, expanded_edges


class SynthesisNode:
    """
    Synthesizes comprehensive answers from accumulated graph data.
    Creates both intermediate summaries and final comprehensive answer.
    """

    def __init__(self, config: GraphAgentConfig):
        self.config = config
        self.llm = config.llm

    async def __call__(self, state: GraphSearchState) -> Dict[str, Any]:
        """Synthesize answer from all retrieved information"""

        current_depth = state.current_depth
        is_final = current_depth >= state.search_depth

        if is_final:
            # Generate comprehensive final answer
            answer = await self._generate_final_answer(state)
            return {
                "final_answer": answer
            }
        else:
            # Generate intermediate synthesis
            intermediate = await self._generate_intermediate_synthesis(state, current_depth)
            # Append to existing syntheses
            updated_syntheses = list(state.intermediate_syntheses)
            updated_syntheses.append(intermediate)
            return {
                "intermediate_syntheses": updated_syntheses
            }

    async def _generate_intermediate_synthesis(
            self,
            state: GraphSearchState,
    ) -> str:
        """Create a summary of findings at current depth"""

        recent_edges = [e for e in state.edges if e.get("depth_discovered") == state.current_depth - 1]
        recent_nodes = [n for n in state.nodes if n.get("depth_discovered") == state.current_depth - 1]

        prompt = f"""
        Based on the following information retrieved from the knowledge graph at depth {state.current_depth}:

        ENTITIES FOUND ({len(recent_nodes)}):
        {self._format_nodes(recent_nodes[:10])}

        FACTS FOUND ({len(recent_edges)}):
        {self._format_edges(recent_edges[:20])}

        Create a brief summary (2-3 sentences) of what was discovered.
        Focus on information relevant to the query: {state.user_query}
        """

        response = await self.llm.ainvoke([HumanMessage(content=prompt)])
        return response.content

    async def _generate_final_answer(self, state: GraphSearchState) -> str:
        """Generate comprehensive final answer from all accumulated data"""

        all_edges = state.edges
        all_nodes = state.nodes
        all_communities = state.communities

        # Check if we have no knowledge to answer the query
        if not all_edges and not all_nodes and not all_communities:
            return "I don't have information about that in the knowledge graph."

        # Build comprehensive prompt
        prompt = f"""
        You are synthesizing a comprehensive answer based on knowledge graph exploration.

        USER QUERY: {state.user_query}

        KNOWLEDGE GRAPH DATA RETRIEVED:

        === ENTITIES ({len(all_nodes)} total) ===
        {self._format_nodes(all_nodes)}

        === FACTS/RELATIONSHIPS ({len(all_edges)} total) ===
        {self._format_edges(all_edges)}

        === COMMUNITIES ({len(all_communities)} total) ===
        {self._format_communities(all_communities)}

        === INTERMEDIATE FINDINGS ===
        {chr(10).join(state.intermediate_syntheses)}

        INSTRUCTIONS:
        1. Synthesize a comprehensive, detailed answer that fully addresses the user's query
        2. Use ONLY the information from the knowledge graph above - do not add external information
        3. Organize information logically with clear sections
        4. Include specific facts, relationships, and entity details
        5. Note temporal information when relevant (valid_at, invalid_at dates)
        6. If information is incomplete, acknowledge gaps clearly
        7. Provide rich context by connecting multiple facts and entities

        CRITICAL: Use ONLY the provided knowledge graph data above. If all ENTITIES, FACTS, and COMMUNITIES sections are empty, respond: 'I don't have information about that in the knowledge graph.' Do NOT speculate or use external knowledge.

        Generate a thorough, well-structured answer. Answer only on the query. Do not reference the data sections directly or provide additional commentary.
        """

        response = await self.llm.ainvoke([HumanMessage(content=prompt)])
        return response.content

    def _format_nodes(self, nodes: List[Dict]) -> str:
        """Format nodes for prompt"""
        if not nodes:
            return "None"

        formatted = []
        for node in nodes:  # Limit for context window
            formatted.append(
                f"- {node.get('name', 'Unknown')}: {node.get('summary', 'No summary')}"
            )

        return "\n".join(formatted)

    def _format_edges(self, edges: List[Dict]) -> str:
        """Format edges/facts for prompt"""
        if not edges:
            return "None"

        formatted = []
        for edge in edges[:50]:  # Limit for context window
            fact = edge.get('fact', '')
            relation = edge.get('name', '')
            temporal = ""
            if edge.get('valid_at'):
                temporal = f" (valid from {edge['valid_at']})"

            formatted.append(f"- [{relation}] {fact}{temporal}")

        if len(edges) > 50:
            formatted.append(f"... and {len(edges) - 50} more facts")

        return "\n".join(formatted)

    def _format_communities(self, communities: List[Dict]) -> str:
        """Format community summaries for prompt"""
        if not communities:
            return "None"

        formatted = []
        for comm in communities[:10]:
            formatted.append(
                f"- {comm.get('name', 'Community')}: {comm.get('summary', 'No summary')}"
            )

        return "\n".join(formatted)


class ReflectionNode:
    """
    Critiques the generated answer for accuracy, completeness, and relevance.
    Provides feedback for regeneration if needed.
    """

    def __init__(self, config: GraphAgentConfig):
        self.config = config
        self.llm = config.llm

    async def __call__(self, state: GraphSearchState) -> Dict[str, Any]:
        """Reflect on answer quality"""

        if not self.config.enable_reflection:
            return {"needs_regeneration": False}

        answer = state.final_answer
        if not answer:
            return {"needs_regeneration": False}

        regeneration_count = state.regeneration_count
        if regeneration_count >= self.config.max_regenerations:
            return {"needs_regeneration": False}

        # Perform reflection
        prompt = f"""
        Critique the following answer for accuracy, completeness, and relevance.

        USER QUERY: {state.user_query}

        GENERATED ANSWER:
        {answer}

        AVAILABLE DATA:
        - {len(state.edges)} facts/relationships
        - {len(state.nodes)} entities
        - {len(state.communities)} communities

        Evaluate:
        1. Does the answer directly address the user's query?
        2. Is all information grounded in the retrieved data?
        3. Are there obvious gaps or missing connections from the available data?
        4. Is the answer well-organized and comprehensive?
        5. Are there any logical inconsistencies or errors?

        CRITICAL: Do NOT suggest adding information not present in the available data. Only improve how existing data addresses the query—focus on clarity, organization, and completeness of what IS available.

        Respond in this format:
        NEEDS_REGENERATION: [YES/NO]
        FEEDBACK: [Specific feedback if YES, or "None" if NO]
        """

        response = await self.llm.ainvoke([HumanMessage(content=prompt)])
        content = response.content

        needs_regen = "NEEDS_REGENERATION: YES" in content

        # Extract feedback
        feedback = ""
        if "FEEDBACK:" in content:
            feedback = content.split("FEEDBACK:")[1].strip()

        return {
            "needs_regeneration": needs_regen,
            "reflection_feedback": feedback if needs_regen else None,
            "regeneration_count": regeneration_count + (1 if needs_regen else 0)
        }


# ==================== GRAPH CONSTRUCTION ====================

def create_graph_search_agent(graphiti):
    """
    Construct the LangGraph workflow for comprehensive graph search.
    :param graphiti:
    """

    # Create configuration
    config = GraphAgentConfig(
        graphiti_client=graphiti,
        search_depth=3,  # 3 rounds of exploration
        max_results_per_round=15,
        enable_reflection=True,
        max_regenerations=2
    )

    # Initialize nodes
    graph_query = GraphQueryNode(config)
    graph_tools = GraphToolsNode(config)
    synthesis = SynthesisNode(config)
    reflection = ReflectionNode(config)

    # Create state graph
    workflow = StateGraph(GraphSearchState)

    # Add nodes
    workflow.add_node("graph_query", graph_query)
    workflow.add_node("graph_tools", graph_tools)
    workflow.add_node("synthesis", synthesis)
    workflow.add_node("reflection", reflection)

    # Define edges
    workflow.set_entry_point("graph_query")
    workflow.add_edge("graph_query", "graph_tools")

    # Conditional edge after graph_tools: continue exploring or synthesize
    def should_continue_exploration(state: GraphSearchState) -> str:
        current_depth = state.current_depth
        max_depth = state.search_depth

        # Early termination: if depth 1 returned no results, skip to synthesis
        if current_depth == 1 and not state.nodes and not state.edges and not state.communities:
            return "synthesis"

        if current_depth < max_depth:
            # Continue exploring
            return "graph_query"
        else:
            # Done exploring, synthesize
            return "synthesis"

    workflow.add_conditional_edges(
        "graph_tools",
        should_continue_exploration,
        {
            "graph_query": "graph_query",
            "synthesis": "synthesis"
        }
    )

    # After synthesis, go to reflection
    workflow.add_edge("synthesis", "reflection")

    # Conditional edge after reflection: regenerate or end
    def should_regenerate(state: GraphSearchState) -> str:
        if state.needs_regeneration:
            return "synthesis"
        else:
            return "end"

    workflow.add_conditional_edges(
        "reflection",
        should_regenerate,
        {
            "synthesis": "synthesis",
            "end": END
        }
    )

    return workflow.compile()
