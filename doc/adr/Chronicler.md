# Chronicler System Design

## Purpose

The Chronicler is the story's memory and conscience. It watches what happens and understands the *narrative implications*—not mechanical state, but artistic state. What has the story promised? What tensions exist? Where is gravity pulling? What is the world doing independently of the protagonist?

The Chronicler serves two masters:
1. **The MC's story** — Threads, promises, stakes, consequences
2. **The world's story** — Events and progressions that happen whether MC is involved or not

---

## Core Responsibilities

### Track Story State

The narrative fabric of the MC's experience:

| Element | Description |
|---------|-------------|
| **Dramatic Questions** | Tensions the story is asking ("Will she discover his betrayal?") |
| **Promises** | Chekhov's guns—things introduced with weight that need payoff |
| **Active Threads** | Plotlines in motion, dormant, or about to intersect |
| **Stakes** | What's concretely at risk, for whom, with what failure condition |
| **Windows** | Time-limited opportunities that are open or closing |

### Track World Momentum

Events and progressions happening independent of MC:

| Element | Description |
|---------|-------------|
| **Name** | Identifying label for the momentum item |
| **Status** | Current state of progression |
| **Trajectory** | Direction (advancing, stalling, accelerating, resolving) |
| **Timeline** | Expected pace (hours, days, weeks) |
| **Last Event** | Most recent development |
| **MC Awareness** | Does MC know about this? (none, rumors, partial, full) |
| **Potential Intersections** | How this might touch MC's story |

### Emit World Events

When something happens—from MC action or world momentum advancement—the Chronicler writes it as a discoverable event in prose form to World KG.

### Check Simulation Impact on Momentum

When simulation emits `world_events_emitted`, those events are committed to World KG by the simulation pipeline. Chronicler receives these events and checks if any affect world momentum items—integrating impacts where relevant.

### Guide the Writer

Provide narrative-aware guidance for the next scene: what threads to weave, what's manifesting, what opportunities exist, where the emotional arc is heading.

### Request Lore

When world momentum implies knowledge that doesn't exist in the Knowledge Graph, request its creation. This is world-driven, not MC-driven.

---

## What Chronicler Is Not

- **Not a tracker** — Doesn't track mechanical state (health, inventory, position)
- **Not a controller** — Doesn't decide what happens, only notices implications
- **Not a propagator** — Doesn't decide who knows what; characters discover through their own queries
- **Not a scene writer** — Provides guidance, Writer makes final creative decisions
- **Not an event committer for simulation** — Simulation pipeline handles committing character-emitted events to KG

---

## Data Structures

### Story State (Persisted)

```json
{
  "dramatic_questions": [
    {
      "question": "Will she discover his betrayal before it's too late?",
      "introduced": "12:00 03-06-845",
      "tension_level": "high",
      "resolution_proximity": "near"
    }
  ],
  
  "promises": [
    {
      "setup": "The sealed letter from his father",
      "introduced": "08:00 01-06-845",
      "time_since": "4 days",
      "payoff_readiness": "ready"
    }
  ],
  
  "active_threads": [
    {
      "name": "Halvard investigation",
      "status": "MC gathering evidence, Halvard becoming aware",
      "momentum": "building",
      "last_touched": "14:00 05-06-845"
    }
  ],
  
  "stakes": [
    {
      "what": "Mira's life",
      "condition": "If MC doesn't return before dawn, she dies in the cells",
      "deadline": "06:00 06-06-845",
      "failure_consequence": "Mira executed, MC loses key ally and carries guilt"
    }
  ],
  
  "windows": [
    {
      "opportunity": "Merchant caravan offers safe passage south",
      "closes": "08:00 08-06-845",
      "if_missed": "Must find alternate route through dangerous territory"
    }
  ],
  
  "world_momentum": [
    {
      "name": "Crimson Veil summoning ritual",
      "status": "Final phase preparation",
      "trajectory": "advancing",
      "timeline": "days",
      "last_event": "Demon heart acquired from northern hunters",
      "last_updated": "Night, 04-06-845",
      "mc_awareness": "none",
      "potential_intersections": [
        "If MC investigates sect activity",
        "If ritual completes near MC location",
        "If summoned entity spreads chaos"
      ]
    },
    {
      "name": "The spreading blight",
      "status": "Fourth village evacuated, spreading south",
      "trajectory": "accelerating",
      "timeline": "ongoing",
      "last_event": "Refugees arriving in Ironhaven",
      "last_updated": "05-06-845",
      "mc_awareness": "rumors",
      "potential_intersections": [
        "Refugees create social tension",
        "Blight reaches area MC travels through",
        "Source investigation becomes relevant"
      ]
    }
  ]
}
```

### World Event (To World KG)

```json
{
  "when": "14:00 05-06-845",
  "where": "Ironhaven, market square",
  "event": "A confrontation erupted in the market square when a young outsider publicly accused Lord Halvard of involvement in smuggling operations. The outsider claimed to have evidence of bribes and illegal shipments. Halvard denied the accusations and departed with his guards. The crowd dispersed but the incident was witnessed by dozens of merchants and shoppers. City watch took no action."
}
```

Events are written in prose—readable, searchable, discoverable by characters based on their interests.

### Writer Guidance

```json
{
  "weave_in": [
    {
      "thread": "Kira's growing trust",
      "status": "She's been testing MC. Recent events have impressed her.",
      "suggestion": "If Kira appears, she's warmer than before—small tells, not exposition"
    }
  ],
  
  "manifesting_now": [
    {
      "cause": "MC was seen leaving the burned warehouse",
      "consequence": "Someone is asking questions about the fire",
      "how_it_appears": "Could be guards investigating, the owner seeking answers, or insurance agents"
    }
  ],
  
  "opportunities_present": [
    {
      "what": "The merchant Tam owes MC a favor",
      "window": "He leaves Ironhaven at 08:00 08-06-845",
      "if_missed": "Favor becomes much harder to collect"
    }
  ],
  
  "tonal_direction": "Tension building. MC has made bold moves, consequences are accumulating. The next few scenes should feel like walls closing in before a decision point.",
  
  "promises_ready": [
    {
      "setup": "The sealed letter from his father",
      "time_since": "4 days",
      "payoff_opportunity": "A quiet moment of reflection, or a crisis that forces him to finally open it"
    }
  ],
  
  "dont_forget": [
    "The guard who recognized MC but stayed silent—his motivation is unresolved",
    "MC promised Mira they'd return before dawn—deadline is 06:00 06-06-845"
  ],
  
  "world_momentum_notes": [
    {
      "item": "Crimson Veil ritual",
      "relevance": "Background tension. MC doesn't know yet. Could manifest as: strange omens, nervous cult members in town, unusual black market purchases.",
      "if_intersects": "Major escalation—this is a potential arc-defining threat"
    },
    {
      "item": "The spreading blight",
      "relevance": "Refugees creating tension in Ironhaven. Could be background color or direct obstacle.",
      "if_intersects": "Environmental threat, resource scarcity, desperate people"
    }
  ]
}
```

### Lore Request

```json
{
  "lore_requests": [
    {
      "reason": "Crimson Veil ritual advancing to final phase",
      "subject": "The Crimson Veil's summoning ritual—requirements, effects, warning signs visible to outsiders",
      "lore_type": "metaphysics",
      "depth": "moderate",
      "narrative_purpose": {
        "immediate": "World momentum item needs substance",
        "long_term": "Creates discoverable threat, potential intersection with characters"
      }
    }
  ]
}
```

Lore requests are world-driven. The ritual is happening; we need to know what that actually means.

---

## Integration Points

### Input Sources

| Source | What Chronicler Receives |
|--------|-------------------------|
| **Current Scene** | What just happened in the narrative |
| **Current Time** | From SceneTracker (DateTime field) |
| **Previous Time** | Time from previous scene (for calculating elapsed time) |
| **Previous Story State** | Chronicler's own persisted state |
| **Story Bible** | Tone, themes, content calibration |
| **Simulation Events** | `world_events_emitted` from character simulations (already committed to KG by simulation pipeline) |

### Output Destinations

| Output | Destination | Purpose |
|--------|-------------|---------|
| **Story State** | Chronicler's persistence | Memory across scenes |
| **World Events** | World KG | Discoverable facts for characters |
| **Writer Guidance** | Writer Agent | Narrative-aware scene crafting |
| **Lore Requests** | LoreCrafter | Fill world knowledge gaps |

---

## System Flow

### After Scene (Parallel Processing)

```
Scene Ends
    │
    ▼
[PARALLEL POST-SCENE PROCESSING]
├── Chronicler
│   ├── Receives: current scene, current time, previous time
│   ├── Calculates time elapsed
│   ├── Analyzes scene for narrative implications
│   ├── Updates story state (threads, promises, stakes, windows)
│   ├── Advances world momentum (time-based)
│   ├── Emits world events → World KG
│   ├── Generates writer guidance → stored for next scene
│   └── Generates lore requests → LoreCrafter queue
│
├── CharacterReflection (per present character)
│   └── Scene rewrites, memories, state updates
│
├── Trackers
│   └── MC state, scene tracker updates
│
└── Simulation (if triggered)
    ├── Characters live their period
    ├── May emit world_events_emitted → committed to World KG by simulation pipeline
    └── Events passed to Chronicler for momentum impact check
```

### Simulation Feedback Loop

```
Simulation Runs
    │
    ├── Character simulations receive world momentum as context
    │   "Crimson Veil activity increasing. Ritual preparations ongoing."
    │   "Lord Halvard publicly accused of smuggling."
    │
    ├── Characters act based on their goals
    │   └── May emit world_events_emitted
    │
    └── Simulation Pipeline
        ├── Commits world_events_emitted → World KG
        └── Passes events to Chronicler
                │
                ▼
Chronicler Receives Simulation Events
    │
    └── Checks if any events affect world momentum
        └── If yes: updates momentum item status/trajectory
            Example: Kira's network reports unusual buyers for ritual components
            → Updates "Crimson Veil ritual" momentum with new intelligence
```

### Next Scene Setup

```
Next Scene Begins
    │
    ▼
Context Gathering
    ├── Queries World KG (includes events from Chronicler and simulation)
    ├── Queries character memories
    └── Retrieves Chronicler's writer guidance
            │
            ▼
Writer Receives
    ├── Resolution output
    ├── Context from ContextGatherer
    ├── Character states
    └── writer_guidance from Chronicler
        ├── What threads to weave
        ├── What's manifesting now
        ├── What opportunities exist
        ├── Tonal direction
        ├── Promises ready for payoff
        ├── Things not to forget
        └── World momentum notes
```

---

## World Momentum Advancement

### Time-Based Progression

Each time the Chronicler runs, it calculates elapsed in-world time and advances momentum items accordingly.

| Timeline | Advancement Trigger |
|----------|---------------------|
| Hours | Significant time passage (6+ hours) |
| Days | Each day of in-world time |
| Weeks | Major time skips, arc transitions |
| Ongoing | Gradual, continuous presence |

### Event-Based Modification

Momentum can be accelerated, stalled, or redirected by:

1. **MC Actions** — Direct intervention or inadvertent impact
2. **Character Actions** — `world_events_emitted` from simulations that affect momentum
3. **Cascading Consequences** — One momentum item affecting another

### Advancement Output

When momentum advances significantly, Chronicler emits a world event:

```json
{
  "when": "Night, 07-06-845",
  "where": "Unknown location, Crimson Veil territory",
  "event": "The Crimson Veil completed preparations for their summoning ritual. Cult members have been recalled from field operations across the region. Livestock deaths and strange lights reported near suspected cult holdings. Local villages are uneasy. The ritual is believed imminent."
}
```

This becomes discoverable. Characters with relevant interests may find it through queries. MC may encounter its effects. The world moves.

---

## Character Discovery

Chronicler does not decide who knows what. Characters discover information through their own agency:

1. **Simulation runs** for a character
2. Character has **goals and concerns** that shape their attention
3. Character's simulation **queries World KG** based on their interests
4. If relevant events exist, they **discover them naturally**

Example:
- Kira cares about smuggling operations and Halvard's influence
- Her simulation queries "Halvard smuggling" or "market incidents"
- She discovers the public accusation event
- Her simulation reacts based on her personality and goals

The Chronicler writes events. Characters find what matters to them.

---

## Writer Relationship

The Writer receives `writer_guidance` but retains creative authority.

### Guidance Is Suggestive, Not Prescriptive

```json
{
  "weave_in": [
    {
      "thread": "Kira's trust",
      "suggestion": "If she appears, she's warmer—small tells"
    }
  ]
}
```

Writer decides:
- Whether Kira appears at all
- How warmth manifests (if at all)
- Whether this scene is the right moment

### Manifesting Consequences Are Stronger

```json
{
  "manifesting_now": [
    {
      "cause": "MC was seen at burned warehouse",
      "consequence": "Someone is asking questions"
    }
  ]
}
```

This should happen, but Writer controls:
- Who is asking (guards, owner, investigators)
- When in the scene it surfaces
- How it complicates or enriches the action

### Tonal Direction Is Context

```json
{
  "tonal_direction": "Tension building. Walls closing in."
}
```

Writer uses this to calibrate:
- Pacing choices
- Atmospheric details
- How much pressure to apply

---

## Interaction with Other Agents

### ContextGatherer

Chronicler emits to World KG. ContextGatherer queries World KG for relevant context. No direct communication—the knowledge graph is the interface.

### CharacterReflection

No direct interaction. Both process scenes in parallel. CharacterReflection handles individual character memories; Chronicler handles narrative-level implications.

### Simulation

**Chronicler → Simulation:**
- World momentum items provided as `world_events` context
- Characters consider these when forming intentions

**Simulation → Chronicler:**
- `world_events_emitted` from character simulations (committed to KG by simulation pipeline)
- Chronicler checks if these affect momentum items
- Updates momentum if character actions have impact

### LoreCrafter

Chronicler requests lore when world momentum implies missing knowledge. LoreCrafter produces the lore, which feeds into World KG for future discovery.

### Writer

Chronicler produces `writer_guidance`. Writer receives it alongside other context. Writer makes final creative decisions.

---

## Summary

The Chronicler watches two stories:
1. The MC's journey—with its promises, tensions, and stakes
2. The world's progression—events that unfold regardless of MC involvement

It emits events in prose form to the World KG, where characters can discover them through their own interests. It receives simulation-emitted events and checks for momentum impacts. It guides the Writer with narrative awareness. It requests lore when the world needs substance.

The Chronicler doesn't control the story. It notices the story—and ensures nothing is forgotten.
