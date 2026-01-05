# NPC Simulation System Design

## Knowledge Graph Architecture

The system maintains strict knowledge boundaries through three separate knowledge graphs:

### World KG (Shared)
- Lore, locations, factions, history
- World events (things that happened in the world)
- Facts anyone could discover or learn about
- Fed by: LoreCrafter, LocationCrafter, simulation `world_events_emitted`

### MC KG (Protagonist)
- Narrative from MC's point of view
- MC's memories and experiences
- What MC knows and has witnessed
- Fed by: Scene generation, MC's perspective

### Character KGs (Per Character)
- Narrative from that character's point of view
- Scene rewrites (their subjective experience of MC scenes)
- Simulation narratives (their off-screen experiences)
- What they know, have witnessed, have concluded
- Fed by: CharacterReflection, Simulation outputs

### Boundary Rules

**Characters cannot access:**
- MC KG (they don't know MC's thoughts)
- Other characters' KGs (they don't know others' private experiences)

**Characters CAN access:**
- World KG (public/discoverable facts)
- Their own KG (their memories)

**When characters emit to World KG:**
- Action has visible consequences (fire, destruction, death)
- Information becomes public or discoverable
- Something changes in shared reality

---

## North Star

**Goal:** NPCs with genuine agency. Characters who act, not just react. Antagonists who scheme without waiting for MC input. Love interests who seek the MC. A world that moves.

**Problem with single-model systems (e.g., SillyTavern):** One model juggles everything, leading to:
- Knowledge bleed between characters
- Memory inconsistency
- Reactive instead of proactive NPCs
- Hooks feel forced rather than emergent

**Our approach:**
- Strong knowledge boundaries (separate KG per important character)
- Emulation at write-time (CharacterPlugin with full context)
- Simulation for off-screen progression (characters live between encounters)

---

## Character Categories

Hard cap: **~8 arc_important characters maximum**

This isn't arbitrary—it's a narrative principle. Stories have limited bandwidth for characters with agency. Most work with 4-6:
- 1-2 antagonists
- 1-2 love interests / close allies  
- 1-2 wildcards (mentor, rival, complex ally)

| Category | Count | Agency | Profile | KG & Memories | Off-Screen | Examples |
|----------|-------|--------|---------|---------------|------------|----------|
| **arc_important** | 4-8 | Active—drives story | Full | Yes | Full simulation | Antagonist with scheme, love interest pursuing MC, ally with own agenda |
| **significant** | 10-20 | Passive—consistent when encountered | Full | Yes | Offscreen inference | Recurring quest-giver, faction representative, established merchants |
| **background** | Unlimited | None—functional roles | Partial | No | Nothing | Guards, bartenders, crowd members |

**Key insight:** arc_important and significant have identical data (full profile, own KG, memories). The only difference is off-screen processing. This makes promotion/demotion trivial—just flip a flag.

### When to Promote/Demote

**Promote significant → arc_important when:**
- Character's independent decisions start affecting the story
- Character develops goals that involve seeking or opposing MC
- Writer flags via `upgrade_requests`

**Demote arc_important → significant when:**
- Character's arc resolves (antagonist defeated, romance settled)
- Character exits the active story area long-term
- Need to make room for newly important character (hard cap)
- Writer flags via `downgrade_requests`

**Writer output format:**

```json
{
  "scene": "...",
  "choices": [...],
  "creation_requests": {...},
  
  "upgrade_requests": [
    {
      "character": "Tam",
      "reason": "His forgery network is becoming central to the smuggling arc, needs independent agency"
    }
  ],
  
  "downgrade_requests": [
    {
      "character": "Old Marcus", 
      "reason": "Arc resolved—made peace with MC, retiring from active scheming"
    }
  ]
}
```

---

## Processing Types

### Full Simulation (arc_important only)

**What it is:** Character lives through a time period, making decisions, pursuing goals, interacting with others.

**When it runs:** After scene, when `current_time - last_simulated > threshold` (default: 6 in-world hours)

**What it produces:**
- **Scenes** — First-person narrative memories from character's POV
- **Memories** — Indexed for retrieval (summary, salience, entities, tags)
- **State updates** — Emotional state, goal progress, arc progression
- **Tracker updates** — Physical state (fatigue, needs, etc.)
- **Relationship updates** — Changes in how they view others
- **Events affecting others** — Logged to significant characters they interacted with
- **pending_mc_interaction** — If they decide to seek the MC
- **potential_interactions** — Intended interactions with other profiled characters (feeds next SimulationPlanner)
- **world_events_emitted** — Facts about the world others could discover (goes to World KG)

### world_events_emitted

When a character's actions create facts about the shared world:

```json
{
  "world_events_emitted": [
    {
      "name": "Warehouse Fire at Pier 7",
      "description": "A fire broke out at the abandoned warehouse on Pier 7 during the night, destroying the building and its contents. Witnesses saw a cloaked figure leaving shortly before."
    }
  ]
}
```

`name` and `description` are required. This goes to the World KG—shared knowledge anyone could discover.

### potential_interactions

When a standalone simulation results in the character wanting to interact with another profiled character:

```json
{
  "potential_interactions": [
    {
      "character": "Tam",
      "intent": "Negotiate payment terms for the manifest job",
      "timing": "Tomorrow morning",
      "location": "His office at the docks",
      "urgency": "medium"
    }
  ]
}
```

This feeds into the next SimulationPlanner cycle. If Tam is also queued for simulation, they get grouped into a cohort.

**Modes:**
- **Standalone** — Character simulated alone, interactions with non-arc characters summarized
- **Cohort** — 2-4 arc_important characters simulated together when goals explicitly intersect

### Offscreen Inference (significant characters)

**What it is:** Lightweight state derivation. Not simulation—no scenes generated, no narrative content.

**When it runs:** On-demand, when character is about to appear in a scene

**Inputs:**
- Character profile and routine
- Last known state
- Events log (things that happened TO them, from arc_important simulations)
- Time elapsed
- World events

**What it produces:**
- **Current situation** — Location, activity, readiness for interaction
- **State snapshot** — Emotional state, physical state
- **Inference summary** — Brief explanation of what they've been doing (not narrative, just reasoning)

**Key insight:** Doesn't replay time. Answers: "Given who they are and what happened to them, where are they now?"

### Partial Profile (background characters)

**What it is:** Minimal character data for voice consistency.

**When it runs:** Created when character first appears, used during scene

**What it produces:**
- Nothing persistent
- Voice and behavioral patterns for Writer to use
- Discarded after scene (unless promoted)

---

## Events Log

When an arc_important character's simulation involves a significant character, we log the event:

```json
{
  "character": "Tam",
  "time": "14:00 05-06-845",
  "event": "Kira came for backdated manifest, tense negotiation about payment",
  "source": "Kira's simulation",
  "implications": "Tam is annoyed, owes Kira less now, might be more cautious"
}
```

When MC later encounters Tam, OffscreenInference consumes this log to derive his current state.

**Cleanup:** Events are deleted after consumption by OffscreenInference. No accumulation.

---

## pending_mc_interaction

When an arc_important character decides during simulation to seek the MC:

```json
{
  "character": "Kira",
  "intent": "Warn MC about incoming Halvard raid",
  "driver": "Overheard guards discussing MC's location, feels indebted",
  "urgency": "high",
  "approach": "Send street kid with message to arrange meeting",
  "emotional_state": "Anxious, conflicted about getting involved",
  "what_i_want": "MC to escape, maybe owe me one",
  "what_i_know": "Raid planned for tomorrow morning, 6 guards, they know MC's inn"
}
```

**Writer consumes this as:**
- **immediate** — Interrupt current scene
- **high** — Weave into scene transition or next scene opening
- **medium** — Find appropriate moment in next few scenes
- **low** — Background thread, address when natural

The NPC initiated. The world moved. Writer controls pacing.

---

## System Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        SCENE ENDS                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                 ┌────────────────────────┐
                 │  Any arc_important     │
                 │  character stale?      │
                 │  (>6h or likely to     │
                 │  appear next scene)    │
                 └────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              │ YES                           │ NO
              ▼                               ▼
┌─────────────────────────┐        ┌──────────────────┐
│   SimulationPlanner     │        │  Skip to next    │
│                         │        │  scene           │
│ - ALL arc_important get │        └──────────────────┘
│   simulated (sync rule) │
│ - Check potential_      │
│   interactions for      │
│   cohort formation      │
│ - Assign cohorts vs     │
│   standalone            │
└─────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    RUN SIMULATIONS                               │
│                                                                  │
│  Cohorts (when interactions pending):  Standalone (default):     │
│  ┌─────────────────┐                  ┌─────────────────┐       │
│  │ SimModerator    │                  │ StandaloneSim   │       │
│  │ orchestrates    │                  │ character lives │       │
│  │ interactions    │                  │ their period    │       │
│  └─────────────────┘                  └─────────────────┘       │
│                                                                  │
│  Outputs:                                                        │
│  - Scenes & memories (→ character's KG)                         │
│  - State updates (→ character profile)                          │
│  - Events affecting significant chars (→ events log)            │
│  - pending_mc_interactions (→ Writer)                           │
│  - potential_interactions (→ next SimulationPlanner cycle)      │
└─────────────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     NEXT SCENE STARTS                            │
└─────────────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    SCENE GENERATION                              │
│                                                                  │
│  Writer receives:                                                │
│  - pending_mc_interactions (hooks to potentially use)            │
│  - Current NPC states                                            │
│  - upgrade_requests / downgrade_requests from previous scene     │
│                                                                  │
│  During scene, for NPC responses:                                │
│  - arc_important → CharacterPlugin (already current)             │
│  - significant → CharacterPlugin (after inference)               │
│  - background → Writer direct (GEARS framework, partial profile) │
└─────────────────────────────────────────────────────────────────┘
```

---

## Simulation Timing

### Triggers

Simulation runs when:
1. At least one arc_important character is stale (>6 in-world hours since last simulation)
2. OR an arc_important character is likely to appear in the next scene

### All-or-Nothing Rule

**If any arc_important character is simulated, ALL arc_important characters are simulated.**

This keeps them synchronized—all living in the same time. No character gets "ahead" or "behind."

### Simulation Period (Dynamic)

SimulationPlanner determines the period based on context:
- **Default:** 6 in-world hours
- **Until natural break:** "Until morning", "Until end of workday"
- **Until likely encounter:** Shorter period if MC is heading toward character's location

### Skip Conditions

Do NOT simulate characters who:
- Were present in the scene that just ended (already up to date)
- Were simulated within the last 2 in-world hours (still fresh)

### Rapid Scene Sequences

If MC has 10 scenes over 30 minutes of in-game time, no simulation runs. Simulation catches up when significant time passes (MC sleeps, travels, time skip).

---

## Cohort Formation

**Cohorts are rare.** Most arc_important characters are doing their own thing.

**Form cohort when:**
- Character A's explicit current goal involves Character B
- OR Character A has a `potential_interaction` targeting Character B (from previous standalone simulation)
- AND both are arc_important
- AND both are queued for simulation

**Maximum cohort size:** 4 (larger becomes unwieldy)

**Scoring heuristics for grouping:**

| Factor | Score |
|--------|-------|
| potential_interaction from previous sim | +4 (automatic cohort) |
| Same specific location | +3 |
| Same general area | +1 |
| Strong relationship (positive or negative) | +2 |
| One's goals explicitly involve the other | +3 |
| Same faction with active shared business | +1 |
| Routine overlap | +1 |

**Threshold:** Pairs scoring 3+ should be in same cohort.

If no pairing scores 3+, character goes standalone.

---

## Agents Summary

| Agent | Purpose | When | Input | Output |
|-------|---------|------|-------|--------|
| **SimulationPlanner** | Decide who simulates and how | After scene (if time threshold met) | Character roster, story state | Cohorts, standalone list, skip list |
| **SimulationModerator** | Orchestrate cohort simulation | During cohort sim | Cohort members, time period | Pass-through character outputs, timeline |
| **CharacterSimulation** | Live as character in group | During cohort sim | Profile, state, queries from Moderator | Scenes, memories, state updates, pending_mc_interaction |
| **StandaloneSimulation** | Live as character alone | During standalone sim | Profile, state, time period | Scenes, memories, state updates, pending_mc_interaction |
| **OffscreenInference** | Derive current state | Before scene (for significant chars) | Profile, routine, events log, time | Current situation, state snapshot |
| **PartialProfileCrafter** | Create lightweight profile | When background char appears | Character request, context | Minimal profile (voice, appearance, behavior) |
| **CharacterPlugin** | Respond as character in scene | During scene generation | Profile, state, stimulus | Character response (internal, action, speech, attention) |
| **CharacterReflection** | Process scene into character's memory | After scene | MC-POV scene, profile, state | Scene rewrite, memory, state updates |

**Note:** CharacterPlugin is used for both arc_important and significant characters during scene generation. The difference is only in off-screen processing (simulation vs inference).

---

## Data Structures

### Character Roster Entry

```json
{
  "name": "Kira",
  "importance": "arc_important",
  "location": "Portside District, Ironhaven",
  "last_simulated": "08:00 05-06-845",
  "goals_summary": "Find new smuggling route, avoid Halvard attention",
  "key_relationships": ["Tam", "Protagonist"],
  "relationship_notes": {
    "Tam": "Business partner, owes him money, trust is transactional",
    "Protagonist": "Uncertain ally, helped me once, watching carefully"
  },
  "routine_summary": "Mornings at docks, afternoons meeting contacts, evenings at Rusty Anchor",
  "potential_interactions": [
    {
      "character": "Tam",
      "intent": "Settle payment dispute",
      "timing": "Tomorrow morning",
      "location": "Docks office",
      "urgency": "medium"
    }
  ]
}
```

**Note:** `potential_interactions` comes from previous standalone simulation output. Empty array if none pending.

### Events Log Entry

```json
{
  "character": "Tam",
  "time": "14:00 05-06-845",
  "event": "Kira came for backdated manifest, tense negotiation about payment",
  "source": "Kira's simulation",
  "implications": "Annoyed, wary, owes Kira less now"
}
```

### pending_mc_interaction

```json
{
  "character": "Kira",
  "intent": "Warn MC about incoming Halvard raid",
  "driver": "Overheard guards, feels indebted to MC",
  "urgency": "high",
  "approach": "Send street kid with message",
  "emotional_state": "Anxious, conflicted",
  "what_i_want": "MC escapes, owes me",
  "what_i_know": "Raid tomorrow morning, 6 guards, they know MC's inn"
}
```

---

## Key Design Decisions

### Why separate KGs per character?

Knowledge boundaries. Each character's memories are their own. No bleed from a single model "knowing" what everyone experienced.

### Why simulation threshold instead of every scene?

Efficiency. Rapid scene sequences (combat, conversations) don't need NPC catch-up. Simulation runs when meaningful time passes.

### Why events log instead of simulating significant characters?

Cost. Full simulation is expensive. Significant characters don't need narrative generation—they need consistent state. Logging events + inference achieves consistency without the overhead.

### Why hard cap on arc_important?

Narrative discipline + compute budget. More than 8 characters with full simulation becomes unwieldy for both the story and the system.

### Why cohorts are rare?

Cost and complexity. Moderator overhead is significant. Most characters, even important ones, are on separate tracks. Cohort only when goals explicitly intersect.