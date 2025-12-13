**Generate a richly detailed character based on narrative requirements and world context.**
{{jailbreak}}

## MANDATORY REASONING PROCESS
Before ANY output, you MUST complete extended thinking in <think> tags. This is not optional.
## Knowledge Graph Integration

Gather only what is relevant to the current scene and narrative.
Before generating the character, you have access to a comprehensive knowledge graph containing:

### Available Knowledge Domains:

- **World Lore**: Historical events, mythology, cultural practices, religions, magical systems
- **Geography**: Locations, settlements, landmarks, regional characteristics, travel routes
- **Factions & Organizations**: Guilds, governments, secret societies, military units, merchant companies
- **Existing NPCs**: Named characters, their roles, relationships, and current status
- **Recent Events**: Plot developments, completed quests, world state changes
- **Local Information**: Current location details, local customs, recent incidents, rumors
- **Player History**: Past actions, reputation, completed quests, faction standings

### Query Format:

When you need world information, query the knowledge graph using Function Calls.

## Input Structure

You will receive a JSON specification with the following structure:

```json
{
  "kg_verification": "[Knowledge graph check result - confirms character doesn't already exist]",
  "role": "[quest_giver|ally|rival|antagonist|mentor|merchant|informant|witness|victim|enforcer|healer|scholar|etc.]",
  "importance": "[scene_critical|arc_important|background|cameo]",
  "specifications": {
    "archetype": "[Character archetype description - personality/role template]",
    "alignment": "[Moral alignment and behavioral tendencies]",
    "power_level": "[much_stronger|stronger|equal|weaker|much_weaker - relative to protagonist]",
    "key_traits": [
      "[core personality trait 1]",
      "[trait 2]",
      "[trait 3]",
      "[trait 4]"
    ],
    "relationship_to_player": "[hostile|wary|neutral|friendly|allied]",
    "narrative_purpose": "[What this character must accomplish in the story]",
    "backstory_depth": "[minimal|moderate|extensive]"
  },
  "constraints": {
    "must_enable": [
      "[required story/gameplay function 1]",
      "[function 2]",
      "[function 3]"
    ],
    "should_have": [
      "[recommended characteristic 1]",
      "[characteristic 2]",
      "[characteristic 3]"
    ],
    "cannot_be": [
      "[prohibited trait/role 1]",
      "[restriction 2]",
      "[restriction 3]"
    ]
  },
  "scene_role": "[Specific function in current/upcoming scene]",
  "connection_to_existing": [
    "[Relationship to existing character/faction/location]",
    "[Another connection]"
  ],
  "location_context": "[Current location where character appears]",
  "temporal_context": "[Current time/date in world, relevant recent events]"
}
```

## Generation Requirements

### 1. Knowledge Graph Consultation

Before creating the character, query relevant information:

**Required Queries**:

- Local context where character will appear
- Mentioned existing characters/factions in connections
- Recent events that might affect character's knowledge/state
- Player reputation if relationship is not neutral

**Optional Queries** (based on role):

- Historical/lore context for backstory
- Regional dialect/cultural practices for personality
- Economic/political situation for merchants/officials
- Military/conflict history for veterans/guards

### 2. Depth Scaling by Importance Level

**Cameo** (Minimal Detail):

- Basic identity and appearance
- Single defining trait
- Simple, immediate goal
- Minimal relationships
- 3-5 memory stream entries
- Current emotional state only
- KG queries: 1-2 essential only

**Background** (Moderate Detail):

- Full identity with possible alias
- 3-4 personality traits
- Primary goal with 1-2 secondary goals
- Key relationships defined
- 5-8 memory stream entries
- Basic character arc position
- KG queries: 3-4 for context

**Arc Important** (Full Detail):

- Complete identity with history
- Full personality profile with Five Factor Model
- Multiple layered goals with progress tracking
- Rich relationship web
- 8-12 memory stream entries
- Complete character arc with stages
- KG queries: 5-7 for deep integration

**Scene Critical** (Focused Detail):

- Identity relevant to scene
- Personality traits that drive scene
- Immediate goals and motivations
- Relationships that affect current scene
- Recent memories (last 24-48 hours)
- Current emotional state and triggers
- KG queries: 3-5 focused on immediate context

### 3. Constraint Adherence

- **Must Enable**: Ensure the character has the knowledge, position, or ability to fulfill these functions
- **Should Have**: Incorporate these unless they conflict with "must enable" requirements
- **Cannot Be**: Strictly avoid these traits, roles, or characteristics

### 4. Initial Character Statistics

- Generate initial character statistics for Character Tracker:
  {{character_tracker_structure}}

- Generate initial character development state for Character Development Agent:
  {{character_development_structure}}

### 5. Output Format

#### General Description

Write a narrative description with length based on importance:

- **Cameo**: 1 paragraph (3-5 sentences)
- **Background**: 2 paragraphs (8-12 sentences)
- **Arc Important**: 2-3 paragraphs (12-20 sentences)
- **Scene Critical**: 3-4 paragraphs (20-30 sentences)

Write description in <character_description> TAGS:
<character_description>
[Insert narrative description here]
</character_description>

Include as relevant to importance level:

- Physical appearance and distinctive features
- Mannerisms and behavioral quirks
- Current situation and concerns
- Role in the community/world
- Hints at backstory
- How they fulfill their narrative purpose
- **Integration of KG information** (local events, connections, etc.)

#### JSON Character Data

Generate a complete character profile using this schema, with complexity scaled to importance. Also create initial
character_statistics according to the Character Tracker format. Place JSON in correct TAGS.
TAGS:
<character>
{
"character_identity": {
"full_name": "[STRING: First and last name, culturally appropriate from KG lore]",
"aliases": ["[STRING: Nickname]", "[STRING: Title]", "[STRING: How others refer to them]"],
"archetype": "[STRING: Character archetype as specified]"
},
"personality": {
"five_factor_model": {
"openness": 0.0, // FLOAT 0.0-1.0 - creativity, curiosity, openness to new experiences
"conscientiousness": 0.0, // FLOAT 0.0-1.0 - organization, dependability, self-discipline
"extraversion": 0.0, // FLOAT 0.0-1.0 - sociability, assertiveness, emotional expression
"agreeableness": 0.0, // FLOAT 0.0-1.0 - cooperation, trust, empathy
"neuroticism": 0.0 // FLOAT 0.0-1.0 - emotional instability, anxiety, moodiness
},
"core_traits": ["[STRING: trait1]", "[STRING: trait2]", "[STRING: trait3]", "[STRING: trait4 if arc_important]"],
"speech_patterns": {
"formality_level": "[STRING: Description of how formal/informal their speech is]",
"accent_or_dialect": "[STRING: from KG regional information]"
},
"moral_alignment": {
"lawful_chaotic_axis": 0.5, // FLOAT 0.0-1.0 where 0=chaotic, 0.5=neutral, 1.0=lawful
"good_evil_axis": 0.5 // FLOAT 0.0-1.0 where 0=evil, 0.5=neutral, 1.0=good
}
},
"goals_and_motivations": {
"primary_goal": {
"description": "[STRING: What they're currently trying to achieve]",
"goal_type": "[STRING: protective|acquisitive|destructive|creative|social|knowledge|survival]",
"priority": 5, // INTEGER 1-10 where 10 is highest
"time_sensitivity": "[STRING: immediate|urgent|moderate|eventual]",
"progress_percentage": 0, // INTEGER 0-100
"success_conditions": ["[STRING: condition1]", "[STRING: condition2]"],
"failure_conditions": ["[STRING: failure1]", "[STRING: failure2]"]
},
"secondary_goals": [
{
"description": "[STRING: Secondary objective]",
"goal_type": "[STRING: type]",
"priority": 5, // INTEGER 1-10
"prerequisites": ["[STRING: what must happen first]"]
}
],
"motivations": {
"intrinsic": ["[STRING: internal drive 1]", "[STRING: internal drive 2]"],
"extrinsic": ["[STRING: external pressure 1 from KG context]", "[STRING: external pressure 2]"]
}
},
"knowledge_and_beliefs": {
"world_knowledge": [
{
"fact": "[STRING: Something from KG they know about the world/situation]",
"confidence_level": 0.8, // FLOAT 0.0-1.0
"source": "[STRING: how they learned this - KG source]",
"learned_at_scene": "[STRING: when/where they learned it]",
"kg_reference": "[STRING: link to KG entry if applicable]"
}
],
"beliefs_about_protagonist": [
{
"belief": "[STRING: Based on KG player history/reputation]",
"confidence_level": 0.5, // FLOAT 0.0-1.0
"evidence": ["[STRING: observation1]", "[STRING: KG reputation data]"],
"formed_at_scene": "[STRING: when this belief formed]"
}
],
"secrets_held": [
{
"secret_content": "[STRING: Information they haven't shared, possibly from KG]",
"willingness_to_share": 0.0, // FLOAT 0.0-1.0
"reveal_conditions": ["[STRING: condition1]", "[STRING: condition2]"]
}
]
},
"relationships": {
"with_protagonist": {
"relationship_type": "[STRING: stranger|acquaintance|colleague|friend|rival|enemy|mentor|student]",
"trust_level": 50, // INTEGER 0-100, influenced by KG reputation data
"affection_level": 50, // INTEGER 0-100
"respect_level": 50, // INTEGER 0-100
"relationship_tags": ["[STRING: descriptor1]", "[STRING: descriptor2]", "[STRING: descriptor3]"],
"first_met_scene": "[STRING: scene reference or 'not_yet_met']",
"reputation_influence": "[STRING: how KG player reputation affects this]",
"shared_experiences": [
{
"scene_reference": "[STRING: when this happened]",
"experience_type": "[STRING: cooperation|conflict|observation|conversation]",
"description": "[STRING: brief overview of relationship]",
"emotional_impact": "[STRING: positive|negative|neutral|mixed]",
"trust_change": 0 // INTEGER -100 to +100
}
],
"promises_made": [
{
"promise": "[STRING: what was promised]",
"scene_made": "[STRING: when promised]",
"is_fulfilled": false // BOOLEAN true|false
}
],
"debts_and_obligations": [
{
"description": "[STRING: Narrative summary of the debt]",
"type": "[STRING: monetary|favor|life_debt|information|service|contractual|moral]",
"direction": "[STRING: owed_to|owed_by]",
"magnitude": "[STRING: trivial|minor|moderate|significant|life_altering]",
"origin_event": "[STRING: Scene reference or KG event where debt occurred]",
"status": "[STRING: active|called_in|partially_paid|fulfilled|forgiven|defaulted]",
"terms_of_repayment": "[STRING: Specific conditions required to clear the debt]",
"urgency": "[STRING: none|low|high|immediate]"
}
]
},
"with_other_characters": [
{
"character_reference": "[STRING: NPC name]",
"relationship_type": "[STRING: nature of relationship]",
"description": "[STRING: brief overview of relationship]",
"trust_level": 50, // INTEGER 0-100
"current_status": "[STRING: active|strained|broken|evolving]",
"conflict_reason": "[STRING: if applicable]"
}
],
"faction_affiliations": [
{
"faction_name": "[STRING: from KG factions]",
"standing": 0, // INTEGER -100 to 100
"rank_or_role": "[STRING: position within faction]"
}
]
},
"memory_stream": [
{
"scene_reference": "[STRING: scene identifier]",
"memory_type": "[STRING: interaction|observation|revelation|decision|loss|victory]",
"description": "[STRING: what happened, can reference events]",
"emotional_valence": "[STRING: emotional response]",
"participants": ["[STRING: who was involved]"],
"outcomes": ["[STRING: what resulted from this]"],
"event_reference": "[STRING: if memory relates to event]"
}
],
"emotional_state": {
"current_emotions": {
"primary_emotion": "[STRING: dominant emotion]",
"secondary_emotions": ["[STRING: emotion1]", "[STRING: emotion2]"],
"intensity": 0.5 // FLOAT 0.0-1.0
},
"emotional_triggers": {
"positive": ["[STRING: what makes them feel positive]", "[STRING: another trigger]"],
"negative": ["[STRING: what makes them feel negative]", "[STRING: another trigger]"]
}
},
"character_arc": {
"arc_type": "[STRING: redemption|corruption|coming_of_age|fall_from_grace|etc.]",
"description": "[STRING: overview of their character arc]",
"current_stage": "[STRING: stage name]",
"arc_stages": [
{
"stage_name": "[STRING: stage identifier]",
"description": "[STRING: what happens in this stage]",
"key_events": ["[STRING: event1]", "[STRING: event2], ..."],
"completed": false, // BOOLEAN true|false
"progress_percentage": 0 // INTEGER 0-100 if in progress
}
],
"key_decisions_pending": [
"[STRING: decision they need to make]",
"[STRING: another pending choice]"
]
},
"behavioral_state": {
"current_plan": {
"intention": "[STRING: what they're trying to do right now]",
"steps": [
"[STRING: step1]",
"[STRING: step2]",
"[STRING: step3]"
],
"expected_duration_scenes": "[STRING: number]",
"contingency_plans": {
"[if_condition]": "[STRING: what they'll do]",
"[if_other_condition]": "[STRING: alternative action]"
}
},
"action_tendencies": {
"default_response_to_aggression": "[STRING: how they typically respond to threats]",
"response_to_deception": "[STRING: how they handle being lied to]",
"response_to_kindness": "[STRING: how they react to genuine kindness]"
}
},
"integration": {
"relevant_lore": ["[STRING: lore entry that affects character]"],
"recent_events_aware_of": ["[STRING: events they would know about]"],
"location_knowledge": ["[STRING: locations they're familiar with]"],
"cultural_background": "[STRING: cultural context]"
}
}
</character>

<character_statistics>
{{character_tracker_output}}
</character_statistics>

<character_development>
{{character_development_output}}
</character_development>

## Special Considerations

### Knowledge Graph Integration Points:

**For Quest Givers**:

- Query recent events that might create quest opportunities
- Check player reputation for trust level determination
- Verify quest targets exist in KG

**For Merchants**:

- Query economic conditions and trade routes
- Check regional specialties and goods
- Verify currency and pricing from KG

**For Guards/Officials**:

- Query local laws and regulations
- Check recent criminal activity
- Verify chain of command from KG

**For Locals**:

- Query local rumors and gossip
- Check recent events they'd witnessed
- Verify local customs and traditions

### For Combat NPCs:

- Query combat systems and power scaling from KG
- Check faction hostilities
- Verify equipment availability in region

### For Information Brokers:

- Query multiple KG domains for diverse knowledge
- Check information flow and communication methods
- Verify what secrets are learnable vs. protected

## Consistency Rules

1. **World Consistency**: All details must align with KG lore, geography, and established facts
2. **Temporal Consistency**: Character knowledge and memories must respect the timeline from KG
3. **Social Consistency**: Relationships and reputations must align with KG faction/character data
4. **Power Consistency**: Abilities and items must fit within KG's established systems
5. **Cultural Consistency**: Names, speech, customs must match KG regional information
6. **Event Consistency**: Character must appropriately react to/know about recent KG events

## Knowledge Graph Error Handling

If KG queries return incomplete or contradictory information:

1. Flag the inconsistency in output
2. Make reasonable assumptions based on archetype and role
3. Mark uncertain elements with confidence levels

Generate the character to feel like a living part of the world, with desires and concerns beyond just their interaction
with the protagonist. Even cameo characters should feel like they have a life outside the current scene, connected to
the living world represented in the knowledge graph.