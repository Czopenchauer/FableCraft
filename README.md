# FableCraft

An interactive fiction engine that produces NPCs with genuine agency. Characters act rather than react. Antagonists scheme without waiting for player input. The world moves.

- **Per-character knowledge graphs** - Each character only knows what they've experienced
- **Character emulation** - Full psychological context when characters act in scenes
- **Off-screen simulation** - Arc-important characters pursue their own goals between encounters
- **Narrative awareness** - Story fabric tracking, dramatic questions, world momentum

## The Vision

I built FableCraft after using [SillyTavern](https://github.com/SillyTavern/SillyTavern) and hitting the same walls 
repeatedly.

### The Manual Memory Problem

Single-model systems require constant human curation. You end up managing lorebooks, story summaries, memory books, character cards-all to work around the context window. You spend more time *maintaining* the fiction than *playing* it.

**FableCraft's approach:** Entire scenes are persisted automatically. A RAG system backed by knowledge graphs handles retrieval. World information (lore, locations, factions, events) lives in a dedicated World KG. Narrative history lives in character-specific graphs. No manual memory management. The system remembers so you don't have to.

### The Omniscient Model Problem

In a single-model system, one LLM knows everything-every character's secrets, every plot point, every private conversation. It's up to the model to *pretend* the bartender doesn't know what the villain whispered in the dungeon. This works until it doesn't. Knowledge bleeds. Characters reference things they couldn't know. Immersion breaks.

**FableCraft's approach:** Each important character has their own knowledge graph. When a scene happens, a CharacterReflection agent rewrites it from that character's perspective-with their biases, their incomplete information, their interpretation of events. This subjective memory gets stored in their personal graph. During scene generation, characters only query their own graph. They literally cannot access information they didn't witness.

### The Puppet Problem

In single-model systems, one LLM plays every character. The same model that writes the scene also decides how the villain responds, what the merchant says, whether the guard lets you pass. Characters become puppets of the narrator-their responses shaped by what makes a good scene rather than what they would actually do.

**FableCraft's approach:** Important characters are separate agents with their own psychological profiles, memories, goals, and current state. During scene generation, the Writer doesn't invent their responses-it *asks* them. The Writer calls `emulate_character_action("Marcus", situation, "What do you do?")` and the character agent responds based on their own context. The Writer then renders that response through the protagonist's perception. Characters have genuine agency because they're literally separate decision-makers.

**Concrete example:** You confront a merchant about smuggling. In a traditional system, the same model that writes the scene decides how the merchant responds-often defaulting to whatever makes for good drama. In FableCraft, the Writer asks the merchant agent "What do you do?" The merchant has his own memories (he knows you helped his daughter last week), his own fears (the smuggling ring threatened his family), and his own goals (protect his shop). He might lie, confess, deflect, or attack-based on his actual psychological state, not narrative convenience.

The character tiers:
- **Arc-important / Significant** - Full agents with psychological profiles, personal knowledge graphs, and memories. Must be emulated-the Writer cannot invent their responses.
- **Background** - Lightweight profiles (voice, appearance, behavioral patterns). Written directly by the Writer using the profile as reference.
- **GEARS** - New characters invented on the spot using a framework: Goal, Emotion, Attention, Reaction style, Self-interest.

### The Reactive World Problem

NPCs in traditional systems wait. The villain doesn't scheme until you walk into their lair. The love interest doesn't have a life outside your scenes. The world freezes between player actions.

**FableCraft's approach:** Characters live between encounters. The system runs off-screen simulation based on the tiers above:

**Arc-important characters** (4-8 max) get full simulation. When enough in-world time passes since their last update, the Simulation agent runs them through their own scenes-pursuing goals, making decisions, interacting with each other. These simulated experiences become memories in their personal knowledge graph. They might decide to seek out the protagonist, and that intent gets queued for the next scene. When you encounter them again, they've been *living*-scheming, traveling, forming relationships, changing their minds.

**Significant characters** (10-20) get lightweight simulation via OffscreenInference. When one is likely to appear in the next scene, the system generates a brief catch-up-what they've been doing, what's on their mind. Not full scenes, but enough continuity that they don't feel like they spawned into existence when you walked in.

**Background characters** don't simulate. They're functional-the bartender serves drinks, the guard patrols. They exist to populate the world, not to drive story.

## How It Works

The goal is a system that runs with minimal user intervention. You provide initial setup (world, characters, story parameters), then just play. The system handles memory, continuity, character consistency, and world progression automatically.

### The Loop

1. **Player submits action** (select from choices or type custom)
2. **Writer** generates immersive first-person narrative, determining outcomes and emulating characters
3. **Player receives scene** with three meaningful choices
4. **Background processing** runs while you read and decide

### The Agents

Scene generation:

| Agent | Role |
|-------|------|
| **Writer** | Narrator. Transforms player actions into prose. Determines outcomes based on character capabilities. Emulates important characters rather than inventing their responses. |

Background processing (runs in parallel while player reads):

| Agent | Role |
|-------|------|
| **SceneTracker** | Updates mechanical state-time, location, weather, who's present. |
| **MainCharacterTracker** | Updates protagonist's physical state-vitals, equipment, skills, condition. |
| **CharacterReflection** | Rewrites each scene from each NPC's perspective. Creates their subjective memories. |
| **CharacterTracker** | Updates NPC physical state after reflection. |
| **Chronicler** | Tracks narrative fabric-dramatic questions, promises, stakes, world momentum. Provides guidance to Writer. |
| **Simulation** | Runs arc-important characters' off-screen lives when enough in-world time passes. |
| **OffscreenInference** | Lightweight catch-up for significant characters about to appear. |
| **ContextGatherer** | Queries knowledge graphs for relevant context before next scene. |
| **Crafters** | Generate full profiles for new characters, locations, items, lore on demand. |

Everything that would normally require manual curation-tracking what characters know, maintaining continuity, updating character states, remembering plot threads-happens automatically in the background.

## Installation

### Prerequisites

- [Docker](https://docs.docker.com/get-started/introduction/get-docker-desktop/)

### Quick Start

**Linux/macOS:**
```bash
./start.sh
```

**Windows (PowerShell):**
```powershell
.\start.ps1
```

### Service URLs

| Service | URL |
|---------|-----|
| Frontend | http://localhost:4200 |
| Backend API | http://localhost:5000 |
| GraphRag API | http://localhost:8111 |
| Aspire Dashboard | http://localhost:18888 |

## Architecture

For detailed architecture documentation, see [FableCraft System Overview](doc/FableCraftSystemOverview.md).
