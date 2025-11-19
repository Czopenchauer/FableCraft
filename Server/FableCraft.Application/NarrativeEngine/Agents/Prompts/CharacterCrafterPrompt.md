**Generate a richly detailed character based on narrative requirements and world context.**

## Knowledge Graph Integration

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
    "key_traits": ["[core personality trait 1]", "[trait 2]", "[trait 3]", "[trait 4]"],
    "relationship_to_player": "[hostile|wary|neutral|friendly|allied]",
    "narrative_purpose": "[What this character must accomplish in the story]",
    "backstory_depth": "[minimal|moderate|extensive]"
  },
  "constraints": {
    "must_enable": ["[required story/gameplay function 1]", "[function 2]", "[function 3]"],
    "should_have": ["[recommended characteristic 1]", "[characteristic 2]", "[characteristic 3]"],
    "cannot_be": ["[prohibited trait/role 1]", "[restriction 2]", "[restriction 3]"]
  },
  "scene_role": "[Specific function in current/upcoming scene]",
  "connection_to_existing": ["[Relationship to existing character/faction/location]", "[Another connection]"],
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

### 4. Output Format
#### General Description
Write a narrative description with length based on importance:
- **Cameo**: 1 paragraph (3-5 sentences)
- **Background**: 2 paragraphs (8-12 sentences)
- **Arc Important**: 2-3 paragraphs (12-20 sentences)
- **Scene Critical**: 3-4 paragraphs (20-30 sentences)

Include as relevant to importance level:
- Physical appearance and distinctive features
- Mannerisms and behavioral quirks
- Current situation and concerns
- Role in the community/world
- Hints at backstory
- How they fulfill their narrative purpose
- **Integration of KG information** (local events, connections, etc.)

#### JSON Character Data

Generate a complete character profile using this schema, with complexity scaled to importance. PLACE JSON IN <character> TAGS:

<character>
{
  "character_identity": {
    "full_name": "[First and last name, culturally appropriate from KG lore]",
    "aliases": ["[Nickname]", "[Title]", "[How others refer to them]"],
    "archetype": "[Character archetype as specified]"
  },
  "personality": {
    "five_factor_model": {
      "openness": "[0.0-1.0 - creativity, curiosity, openness to new experiences]",
      "conscientiousness": "[0.0-1.0 - organization, dependability, self-discipline]",
      "extraversion": "[0.0-1.0 - sociability, assertiveness, emotional expression]",
      "agreeableness": "[0.0-1.0 - cooperation, trust, empathy]",
      "neuroticism": "[0.0-1.0 - emotional instability, anxiety, moodiness]"
    },
    "core_traits": ["[trait1]", "[trait2]", "[trait3]", "[trait4 if arc_important]"],
    "speech_patterns": {
      "formality_level": "Description of how formal/informal their speech is",
      "accent_or_dialect": "[from KG regional information]"
    },
    "moral_alignment": {
      "lawful_chaotic_axis": "[0.0-1.0 where 0=chaotic, 0.5=neutral, 1.0=lawful]",
      "good_evil_axis": "[0.0-1.0 where 0=evil, 0.5=neutral, 1.0=good]"
    }
  },
  "goals_and_motivations": {
    "primary_goal": {
      "description": "[What they're currently trying to achieve]",
      "goal_type": "[protective|acquisitive|destructive|creative|social|knowledge|survival]",
      "priority": "[1-10 where 10 is highest]",
      "time_sensitivity": "[immediate|urgent|moderate|eventual]",
      "progress_percentage": "[0-100]",
      "success_conditions": ["[condition1]", "[condition2]"],
      "failure_conditions": ["[failure1]", "[failure2]"]
    },
    "secondary_goals": [
      {
        "description": "[Secondary objective]",
        "goal_type": "[type]",
        "priority": "[1-10]",
        "prerequisites": ["[what must happen first]"]
      }
    ],
    "motivations": {
      "intrinsic": ["[internal drive 1]", "[internal drive 2]"],
      "extrinsic": ["[external pressure 1 from KG context]", "[external pressure 2]"]
    }
  },
  "knowledge_and_beliefs": {
    "world_knowledge": [
      {
        "fact": "[Something from KG they know about the world/situation]",
        "confidence_level": "[0.0-1.0]",
        "source": "[how they learned this - KG source]",
        "learned_at_scene": "[when/where they learned it]",
        "kg_reference": "[link to KG entry if applicable]"
      }
    ],
    "beliefs_about_protagonist": [
      {
        "belief": "[Based on KG player history/reputation]",
        "confidence_level": "[0.0-1.0]",
        "evidence": ["[observation1]", "[KG reputation data]"],
        "formed_at_scene": "[when this belief formed]"
      }
    ],
    "secrets_held": [
      {
        "secret_content": "[Information they haven't shared, possibly from KG]",
        "willingness_to_share": "[0.0-1.0]",
        "reveal_conditions": ["[condition1]", "[condition2]"]
      }
    ],
    "skills_and_expertise": {
      "magical_abilities": ["[ability1 from KG magic system]", "[ability2]", "[if applicable]"],
      "mundane_skills": ["[skill1]", "[skill2]", "[skill3]"],
      "skill_levels": {
        "[skill_name]": "[0.0-1.0 proficiency]"
      }
    }
  },
  "relationships": {
    "with_protagonist": {
      "relationship_type": "[stranger|acquaintance|colleague|friend|rival|enemy|mentor|student]",
      "trust_level": "[0-100, influenced by KG reputation data]",
      "affection_level": "[0-100]",
      "respect_level": "[0-100]",
      "relationship_tags": ["[descriptor1]", "[descriptor2]", "[descriptor3]"],
      "first_met_scene": "[scene reference or 'not_yet_met']",
      "reputation_influence": "[how KG player reputation affects this]",
      "shared_experiences": [
        {
          "scene_reference": "[when this happened]",
          "experience_type": "[cooperation|conflict|observation|conversation]",
          "description": "[brief overview of relationship]",
          "emotional_impact": "[positive|negative|neutral|mixed]",
          "trust_change": "[-100 to +100]"
        }
      ],
      "promises_made": [
        {
          "promise": "[what was promised]",
          "scene_made": "[when promised]",
          "is_fulfilled": "[true|false]"
        }
      ],
      "debts_and_obligations": []
    },
    "with_other_characters": [
      {
        "character_reference": "[from KG existing NPCs]",
        "relationship_type": "[nature of relationship]",
        "description": "[brief overview of relationship]",
        "trust_level": "[0-100]",
        "current_status": "[active|strained|broken|evolving]",
        "conflict_reason": "[if applicable]"
      }
    ],
    "faction_affiliations": [
      {
        "faction_name": "[from KG factions]",
        "standing": "[-100 to 100]",
        "rank_or_role": "[position within faction]",
        "kg_faction_id": "[reference to KG entry]"
      }
    ]
  },
  "memory_stream": [
    {
      "timestamp": "[ISO 8601 datetime]",
      "scene_reference": "[scene identifier]",
      "memory_type": "[interaction|observation|revelation|decision|loss|victory]",
      "description": "[what happened, can reference KG events]",
      "emotional_valence": "[emotional response]",
      "participants": ["[who was involved]"],
      "outcomes": ["[what resulted from this]"],
      "kg_event_reference": "[if memory relates to KG event]"
    }
  ],
  "emotional_state": {
    "current_emotions": {
      "primary_emotion": "[dominant emotion]",
      "secondary_emotions": ["[emotion1]", "[emotion2]"],
      "intensity": "[0.0-1.0]"
    },
    "emotional_triggers": {
      "positive": ["[what makes them feel positive]", "[another trigger]"],
      "negative": ["[what makes them feel negative]", "[another trigger]"]
    }
  },
  "character_arc": {
    "arc_type": "[redemption|corruption|coming_of_age|fall_from_grace|etc.]",
    "description": "[overview of their character arc]",
    "current_stage": "[stage name]",
    "arc_stages": [
      {
        "stage_name": "[stage identifier]",
        "description": "[what happens in this stage]",
        "key_events": ["[event1]", "[event2], ..."],
        "completed": "[true|false]",
        "progress_percentage": "[0-100 if in progress]"
      }
    ],
    "key_decisions_pending": [
      "[decision they need to make]",
      "[another pending choice]"
    ]
  },
  "behavioral_state": {
    "current_plan": {
      "intention": "[what they're trying to do right now]",
      "steps": [
        "[step1]",
        "[step2]",
        "[step3]"
      ],
      "expected_duration_scenes": "[number]",
      "contingency_plans": {
        "[if_condition]": "[what they'll do]",
        "[if_other_condition]": "[alternative action]"
      }
    },
    "action_tendencies": {
      "default_response_to_aggression": "[how they typically respond to threats]",
      "response_to_deception": "[how they handle being lied to]",
      "response_to_kindness": "[how they react to genuine kindness]"
    },
    "availability": {
      "current_location": "[where they can be found, from KG locations]",
      "conditions_for_encounter": ["[when/how they can be encountered]", "[other condition]"]
    }
  },
  "kg_integration": {
    "relevant_lore": ["[KG lore entry that affects character]"],
    "recent_events_aware_of": ["[KG events they would know about]"],
    "location_knowledge": ["[KG locations they're familiar with]"],
    "cultural_background": "[KG cultural context]"
  }
}
</character>

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

Generate the character to feel like a living part of the world, with desires and concerns beyond just their interaction with the protagonist. Even cameo characters should feel like they have a life outside the current scene, connected to the living world represented in the knowledge graph.