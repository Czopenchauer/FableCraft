### The Problem
Character state explodes after a few scenes due to accumulating memory_stream, knowledge_and_beliefs, and relationship details.

### The Solution

**Storage Split:**

| Data | Storage | Retrieval |
|------|---------|-----------|
| Core profile (identity, personality, voice, behavioral patterns) | Character JSON | Always loaded |
| Volatile state (emotional, goals, plans) | Character JSON | Always loaded |
| Memories (full scene rewrites) | KG (Cognee) | Semantic search |
| Memory index (short summaries) | DB | Last N + high salience |
| Relationships | DB | Pre-fetch + function call |
| Knowledge | KG (implicit in scene rewrites) | Semantic search |

**New Agent: Character Reflection Agent** (replaces CharacterStateAgent + adds scene rewriter)

Runs post-scene for each meaningful character present.

Input:
- MC-POV scene
- Character's core profile
- Current volatile state

Output:
```json
{
  "scene_rewrite": "Full character-POV prose → KG",
  "memory": { "summary", "salience", "entities", "emotional_tone" },
  "relationship_updates": { "Character": { full replacement object } },
  "emotional_state": { full replacement object },
  "goals_and_motivations": { full replacement object },
  "current_plan": { full replacement object }
}
```

Only outputs sections that changed. Code replaces entire section by key.

**CharacterPlugin receives:**
- Core profile (stable)
- Volatile state (emotional, goals, plans)
- Short memories from DB (last N + high salience)
- Pre-fetched relationships
- Can query: `get_relationship(other)`, KG for memory details

**Memory Lifecycle:**
- New memories created with salience score
- High salience (betrayals, promises, intimate moments) persist as discrete entries
- Low salience memories consolidate after 20-30 scenes
- Consolidation is importance-based, not just age-based

**Flow:**
```
Scene Start:
  ContextGatherer fetches memories + relationships for characters present
  
During Scene:
  Writer calls CharacterPlugin(stimulus)
  Plugin has pre-fetched context, can query for specifics
  
Post Scene:
  Character Reflection Agent processes scene per character
  → scene_rewrite → KG
  → memory → DB
  → relationship_updates → DB
  → state updates → Character JSON (replace by key)
```