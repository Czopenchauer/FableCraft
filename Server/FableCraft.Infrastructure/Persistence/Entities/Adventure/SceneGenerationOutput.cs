using System.Text.Json.Serialization;

namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public class NarrativeDirectorOutput
{
    [JsonPropertyName("extra_context_gathered")]
    public List<ExtraContext> ExtraContextGathered { get; set; } = new();

    [JsonPropertyName("character_emulation_results")]
    public List<CharacterEmulationResult> CharacterEmulationResults { get; set; } = new();

    [JsonPropertyName("action_processing")]
    public ActionProcessing ActionProcessing { get; set; } = new();

    [JsonPropertyName("continuity_check")]
    public ContinuityCheck ContinuityCheck { get; set; } = new();

    [JsonPropertyName("scene_metadata")]
    public SceneMetadata SceneMetadata { get; set; } = new();

    [JsonPropertyName("objectives")]
    public Objectives Objectives { get; set; } = new();

    [JsonPropertyName("conflicts")]
    public Conflicts Conflicts { get; set; } = new();

    [JsonPropertyName("story_threads")]
    public StoryThreads StoryThreads { get; set; } = new();

    [JsonPropertyName("creation_requests")]
    public CreationRequests CreationRequests { get; set; } = new();

    [JsonPropertyName("scene_direction")]
    public SceneDirection SceneDirection { get; set; } = new();

    [JsonPropertyName("consequences_queue")]
    public ConsequencesQueue ConsequencesQueue { get; set; } = new();

    [JsonPropertyName("pacing_calibration")]
    public PacingCalibration PacingCalibration { get; set; } = new();

    [JsonPropertyName("continuity_notes")]
    public ContinuityNotes ContinuityNotes { get; set; } = new();

    [JsonPropertyName("world_evolution")]
    public WorldEvolution WorldEvolution { get; set; } = new();

    [JsonPropertyName("meta_narrative")]
    public MetaNarrative MetaNarrative { get; set; } = new();
}

public class ExtraContext
{
    [JsonPropertyName("knowledge")]
    public string Knowledge { get; set; } = string.Empty;

    [JsonPropertyName("key_findings")]
    public string KeyFindings { get; set; } = string.Empty;
}

public class CharacterEmulationResult
{
    [JsonPropertyName("character_name")]
    public string CharacterName { get; set; } = string.Empty;

    [JsonPropertyName("situation_provided")]
    public string SituationProvided { get; set; } = string.Empty;

    [JsonPropertyName("assessed_disposition")]
    public string AssessedDisposition { get; set; } = string.Empty; // cooperative|hostile|neutral|opportunistic|conflicted

    [JsonPropertyName("likely_behavior")]
    public string LikelyBehavior { get; set; } = string.Empty;

    [JsonPropertyName("own_goals_this_scene")]
    public string OwnGoalsThisScene { get; set; } = string.Empty;

    [JsonPropertyName("potential_actions")]
    public List<string> PotentialActions { get; set; } = new();

    [JsonPropertyName("cooperation_likelihood")]
    public string CooperationLikelihood { get; set; } = string.Empty; // high|medium|low|none

    [JsonPropertyName("factors_that_could_shift")]
    public string FactorsThatCouldShift { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string Note { get; set; } = "Writer's real-time emulation is authoritative";
}

public class ActionProcessing
{
    [JsonPropertyName("raw_player_action")]
    public string RawPlayerAction { get; set; } = string.Empty;

    [JsonPropertyName("extracted_actions")]
    public List<ExtractedAction>? ExtractedActions { get; set; } = new();

    [JsonPropertyName("wishful_elements")]
    public WishfulElements? WishfulElements { get; set; } = new();

    [JsonPropertyName("chain_result")]
    public string ChainResult { get; set; } = string.Empty;

    [JsonPropertyName("overall_scene_impact")]
    public string OverallSceneImpact { get; set; } = string.Empty;
}

public class ExtractedAction
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("required_skill")]
    public string RequiredSkill { get; set; } = string.Empty;

    [JsonPropertyName("required_resources")]
    public string[] RequiredResources { get; set; } = [];

    [JsonPropertyName("player_capability_assessment")]
    public string PlayerCapabilityAssessment { get; set; } = string.Empty;

    [JsonPropertyName("challenge_tier")]
    public string ChallengeTier { get; set; } = string.Empty;

    [JsonPropertyName("outcome")]
    public string Outcome { get; set; } = string.Empty; // success|partial_success|failure|dangerous_failure|impossible

    [JsonPropertyName("outcome_reasoning")]
    public string OutcomeReasoning { get; set; } = string.Empty;

    [JsonPropertyName("narrative_instruction")]
    public string NarrativeInstruction { get; set; } = string.Empty;
}

public class WishfulElements
{
    [JsonPropertyName("detected_hopes")]
    public string DetectedHopes { get; set; } = string.Empty;

    [JsonPropertyName("treatment")]
    public string Treatment { get; set; } = string.Empty;
}

public class ContinuityCheck
{
    [JsonPropertyName("last_scene_ended")]
    public string LastSceneEnded { get; set; } = string.Empty;

    [JsonPropertyName("ongoing_elements")]
    public OngoingElements OngoingElements { get; set; } = new();

    [JsonPropertyName("this_scene_opens")]
    public string ThisSceneOpens { get; set; } = string.Empty;
}

public class OngoingElements
{
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("active_conversation")]
    public string ActiveConversation { get; set; } = string.Empty;

    [JsonPropertyName("present_npcs")]
    public string PresentNpcs { get; set; } = string.Empty;

    [JsonPropertyName("unresolved_action")]
    public string UnresolvedAction { get; set; } = string.Empty;

    [JsonPropertyName("active_status_effects")]
    public string ActiveStatusEffects { get; set; } = string.Empty;
}

public class SceneMetadata
{
    [JsonPropertyName("scene_number")]
    public int SceneNumber { get; set; }

    [JsonPropertyName("narrative_act")]
    public string NarrativeAct { get; set; } = string.Empty; // setup|rising_action|climax|falling_action|resolution

    [JsonPropertyName("beat_type")]
    public string BeatType { get; set; } = string.Empty; // discovery|challenge|choice_point|revelation|transformation|respite

    [JsonPropertyName("tension_level")]
    public int TensionLevel { get; set; }

    [JsonPropertyName("pacing")]
    public string Pacing { get; set; } = string.Empty; // slow|building|intense|cooldown

    [JsonPropertyName("emotional_target")]
    public string EmotionalTarget { get; set; } = string.Empty; // fear|joy|surprise|sadness|triumph|curiosity|tension
}

public class Objectives
{
    [JsonPropertyName("long_term")]
    public List<LongTermObjective> LongTerm { get; set; } = new();

    [JsonPropertyName("mid_term")]
    public List<MidTermObjective> MidTerm { get; set; } = new();

    [JsonPropertyName("short_term")]
    public List<ShortTermObjective> ShortTerm { get; set; } = new();
}

public class LongTermObjective
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // active|dormant|completed|failed

    [JsonPropertyName("progress_percentage")]
    public int ProgressPercentage { get; set; }

    [JsonPropertyName("stakes")]
    public string Stakes { get; set; } = string.Empty;

    [JsonPropertyName("milestones_completed")]
    public List<string> MilestonesCompleted { get; set; } = new();

    [JsonPropertyName("milestones_remaining")]
    public List<string> MilestonesRemaining { get; set; } = new();
}

public class MidTermObjective
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parent_objective")]
    public string ParentObjective { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("urgency")]
    public string Urgency { get; set; } = string.Empty;

    [JsonPropertyName("progress_percentage")]
    public int ProgressPercentage { get; set; }

    [JsonPropertyName("required_steps")]
    public List<string> RequiredSteps { get; set; } = new();

    [JsonPropertyName("steps_completed")]
    public List<string> StepsCompleted { get; set; } = new();

    [JsonPropertyName("estimated_scenes_remaining")]
    public int EstimatedScenesRemaining { get; set; }

    [JsonPropertyName("npc_cooperation_required")]
    public string NpcCooperationRequired { get; set; } = string.Empty;
}

public class ShortTermObjective
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parent_objective")]
    public string ParentObjective { get; set; } = string.Empty;

    [JsonPropertyName("can_complete_this_scene")]
    public bool CanCompleteThisScene { get; set; }

    [JsonPropertyName("player_capable")]
    public bool PlayerCapable { get; set; }

    [JsonPropertyName("npc_cooperation_needed")]
    public string NpcCooperationNeeded { get; set; } = string.Empty;

    [JsonPropertyName("npc_cooperation_likelihood")]
    public string NpcCooperationLikelihood { get; set; } = string.Empty;

    [JsonPropertyName("alternative_paths")]
    public string AlternativePaths { get; set; } = string.Empty;

    [JsonPropertyName("urgency")]
    public string Urgency { get; set; } = string.Empty; // immediate|pressing|background

    [JsonPropertyName("expiry_in_scenes")]
    public int ExpiryInScenes { get; set; }

    [JsonPropertyName("failure_consequence")]
    public string FailureConsequence { get; set; } = string.Empty;
}

public class Conflicts
{
    [JsonPropertyName("immediate_danger")]
    public ImmediateDanger ImmediateDanger { get; set; } = new();

    [JsonPropertyName("emerging_threats")]
    public List<EmergingThreat> EmergingThreats { get; set; } = new();

    [JsonPropertyName("looming_threats")]
    public List<LoomingThreat> LoomingThreats { get; set; } = new();
}

public class ImmediateDanger
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("threat_level")]
    public int ThreatLevel { get; set; }

    [JsonPropertyName("threat_behavior")]
    public string ThreatBehavior { get; set; } = string.Empty;

    [JsonPropertyName("can_be_avoided")]
    public bool CanBeAvoided { get; set; }

    [JsonPropertyName("resolution_options")]
    public List<string> ResolutionOptions { get; set; } = new();
}

public class EmergingThreat
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("scenes_until_active")]
    public int ScenesUntilActive { get; set; }

    [JsonPropertyName("trigger_condition")]
    public string TriggerCondition { get; set; } = string.Empty;

    [JsonPropertyName("threat_level")]
    public int ThreatLevel { get; set; }
}

public class LoomingThreat
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("current_distance")]
    public string CurrentDistance { get; set; } = string.Empty; // far|approaching|near

    [JsonPropertyName("escalation_rate")]
    public string EscalationRate { get; set; } = string.Empty; // slow|moderate|fast

    [JsonPropertyName("player_awareness")]
    public bool PlayerAwareness { get; set; }
}

public class StoryThreads
{
    [JsonPropertyName("active")]
    public List<ActiveThread> Active { get; set; } = new();

    [JsonPropertyName("seeds_available")]
    public List<ThreadSeed> SeedsAvailable { get; set; } = new();
}

public class ActiveThread
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // opening|developing|ready_to_close|background

    [JsonPropertyName("user_investment")]
    public int UserInvestment { get; set; }

    [JsonPropertyName("scenes_active")]
    public int ScenesActive { get; set; }

    [JsonPropertyName("next_development")]
    public string NextDevelopment { get; set; } = string.Empty;

    [JsonPropertyName("connection_to_main")]
    public string ConnectionToMain { get; set; } = string.Empty;
}

public class ThreadSeed
{
    [JsonPropertyName("trigger")]
    public string Trigger { get; set; } = string.Empty;

    [JsonPropertyName("thread_name")]
    public string ThreadName { get; set; } = string.Empty;

    [JsonPropertyName("potential_value")]
    public string PotentialValue { get; set; } = string.Empty; // low|medium|high
}

public class CreationRequests
{
    [JsonPropertyName("characters")]
    public List<CharacterRequest> Characters { get; set; } = new();

    [JsonPropertyName("lore")]
    public List<LoreRequest> Lore { get; set; } = new();

    [JsonPropertyName("items")]
    public List<ItemRequest> Items { get; set; } = new();

    [JsonPropertyName("locations")]
    public List<LocationRequest> Locations { get; set; } = new();
}

public class CharacterRequest
{
    [JsonPropertyName("kg_verification")]
    public string KgVerification { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty; // required|optional

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("importance")]
    public string Importance { get; set; } = string.Empty; // scene_critical|arc_important|background|cameo

    [JsonPropertyName("specifications")]
    public CharacterSpecifications Specifications { get; set; } = new();

    [JsonPropertyName("constraints")]
    public CharacterConstraints Constraints { get; set; } = new();

    [JsonPropertyName("scene_role")]
    public string SceneRole { get; set; } = string.Empty;

    [JsonPropertyName("connection_to_existing")]
    public List<string> ConnectionToExisting { get; set; } = new();
}

public class CharacterSpecifications
{
    [JsonPropertyName("archetype")]
    public string Archetype { get; set; } = string.Empty;

    [JsonPropertyName("alignment")]
    public string Alignment { get; set; } = string.Empty;

    [JsonPropertyName("power_level")]
    public string PowerLevel { get; set; } = string.Empty; // much_weaker|weaker|equal|stronger|much_stronger

    [JsonPropertyName("key_traits")]
    public List<string> KeyTraits { get; set; } = new();

    [JsonPropertyName("relationship_to_player")]
    public string RelationshipToPlayer { get; set; } = string.Empty;

    [JsonPropertyName("narrative_purpose")]
    public string NarrativePurpose { get; set; } = string.Empty;

    [JsonPropertyName("backstory_depth")]
    public string BackstoryDepth { get; set; } = string.Empty; // minimal|moderate|extensive

    [JsonPropertyName("own_goals")]
    public List<string> OwnGoals { get; set; } = new();
}

public class CharacterConstraints
{
    [JsonPropertyName("must_enable")]
    public List<string> MustEnable { get; set; } = new();

    [JsonPropertyName("should_have")]
    public List<string> ShouldHave { get; set; } = new();

    [JsonPropertyName("cannot_be")]
    public List<string> CannotBe { get; set; } = new();
}

public class LoreRequest
{
    [JsonPropertyName("kg_verification")]
    public string KgVerification { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty; // required|optional

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty; // location_history|item_origin|faction_background|world_event|magic_system|culture|religion|prophecy

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("depth")]
    public string Depth { get; set; } = string.Empty; // brief|moderate|extensive

    [JsonPropertyName("tone")]
    public string Tone { get; set; } = string.Empty;

    [JsonPropertyName("narrative_purpose")]
    public string NarrativePurpose { get; set; } = string.Empty;

    [JsonPropertyName("connection_points")]
    public List<string> ConnectionPoints { get; set; } = new();

    [JsonPropertyName("reveals")]
    public string Reveals { get; set; } = string.Empty;

    [JsonPropertyName("consistency_requirements")]
    public List<string> ConsistencyRequirements { get; set; } = new();
}

public class ItemRequest
{
    [JsonPropertyName("kg_verification")]
    public string KgVerification { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty; // required|optional

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // weapon|armor|consumable|quest_item|artifact|tool|currency|document

    [JsonPropertyName("narrative_purpose")]
    public string NarrativePurpose { get; set; } = string.Empty;

    [JsonPropertyName("power_level")]
    public string PowerLevel { get; set; } = string.Empty; // mundane|uncommon|rare|legendary|unique

    [JsonPropertyName("properties")]
    public ItemProperties Properties { get; set; } = new();

    [JsonPropertyName("must_enable")]
    public List<string> MustEnable { get; set; } = new();

    [JsonPropertyName("acquisition_method")]
    public string AcquisitionMethod { get; set; } = string.Empty; // found|given|purchased|looted|crafted

    [JsonPropertyName("lore_significance")]
    public string LoreSignificance { get; set; } = string.Empty; // low|medium|high
}

public class ItemProperties
{
    [JsonPropertyName("magical")]
    public bool Magical { get; set; }

    [JsonPropertyName("unique")]
    public bool Unique { get; set; }

    [JsonPropertyName("tradeable")]
    public bool Tradeable { get; set; }
}

public class LocationRequest
{
    [JsonPropertyName("kg_verification")]
    public string KgVerification { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty; // required|optional

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // settlement|dungeon|wilderness|landmark|structure|realm

    [JsonPropertyName("scale")]
    public string Scale { get; set; } = string.Empty; // room|building|district|area|region

    [JsonPropertyName("atmosphere")]
    public string Atmosphere { get; set; } = string.Empty;

    [JsonPropertyName("strategic_importance")]
    public string StrategicImportance { get; set; } = string.Empty;

    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();

    [JsonPropertyName("inhabitant_types")]
    public List<string> InhabitantTypes { get; set; } = new();

    [JsonPropertyName("danger_level")]
    public int DangerLevel { get; set; }

    [JsonPropertyName("accessibility")]
    public string Accessibility { get; set; } = string.Empty; // open|restricted|hidden|forbidden

    [JsonPropertyName("connection_to")]
    public List<string> ConnectionTo { get; set; } = new();

    [JsonPropertyName("parent_location")]
    public string ParentLocation { get; set; } = string.Empty;
}

public class SceneDirection
{
    [JsonPropertyName("opening_focus")]
    public string OpeningFocus { get; set; } = string.Empty;

    [JsonPropertyName("player_action_outcome")]
    public PlayerActionOutcome PlayerActionOutcome { get; set; } = new();

    [JsonPropertyName("player_hope_as_inner_monologue")]
    public PlayerHopeAsInnerMonologue PlayerHopeAsInnerMonologue { get; set; } = new();

    [JsonPropertyName("player_state_to_reflect")]
    public PlayerStateToReflect PlayerStateToReflect { get; set; } = new();

    [JsonPropertyName("npc_context")]
    public List<NpcContext> NpcContext { get; set; } = new();

    [JsonPropertyName("narrative_goals")]
    public List<NarrativeGoal> NarrativeGoals { get; set; } = new();

    [JsonPropertyName("npc_own_agendas")]
    public List<NpcOwnAgenda> NpcOwnAgendas { get; set; } = new();

    [JsonPropertyName("search_and_discovery_results")]
    public SearchAndDiscoveryResults SearchAndDiscoveryResults { get; set; } = new();

    [JsonPropertyName("required_elements")]
    public List<string> RequiredElements { get; set; } = new();

    [JsonPropertyName("tone_guidance")]
    public string ToneGuidance { get; set; } = string.Empty;

    [JsonPropertyName("pacing_notes")]
    public string PacingNotes { get; set; } = string.Empty;

    [JsonPropertyName("sensory_details_to_include")]
    public SensoryDetails SensoryDetailsToInclude { get; set; } = new();

    [JsonPropertyName("key_elements_to_describe")]
    public List<string> KeyElementsToDescribe { get; set; } = new();

    [JsonPropertyName("worldbuilding_opportunity")]
    public string WorldbuildingOpportunity { get; set; } = string.Empty;

    [JsonPropertyName("foreshadowing")]
    public List<string> Foreshadowing { get; set; } = new();
}

public class PlayerActionOutcome
{
    [JsonPropertyName("outcome_type")]
    public string OutcomeType { get; set; } = string.Empty; // success|partial_success|failure|dangerous_failure|impossible

    [JsonPropertyName("narrative_direction")]
    public string NarrativeDirection { get; set; } = string.Empty;

    [JsonPropertyName("show_attempt")]
    public bool ShowAttempt { get; set; }

    [JsonPropertyName("consequence_to_depict")]
    public string ConsequenceToDepict { get; set; } = string.Empty;
}

public class PlayerHopeAsInnerMonologue
{
    [JsonPropertyName("hopes_detected")]
    public string HopesDetected { get; set; } = string.Empty;

    [JsonPropertyName("inner_monologue_direction")]
    public string InnerMonologueDirection { get; set; } = string.Empty;
}

public class SearchAndDiscoveryResults
{
    [JsonPropertyName("search_performed")]
    public bool SearchPerformed { get; set; }

    [JsonPropertyName("items_found")]
    public List<string> ItemsFound { get; set; } = new();

    [JsonPropertyName("items_not_found")]
    public List<string> ItemsNotFound { get; set; } = new();

    [JsonPropertyName("discovery_instruction")]
    public string DiscoveryInstruction { get; set; } = string.Empty;
}

public class SensoryDetails
{
    [JsonPropertyName("sight")]
    public string Sight { get; set; } = string.Empty;

    [JsonPropertyName("sound")]
    public string Sound { get; set; } = string.Empty;

    [JsonPropertyName("smell")]
    public string Smell { get; set; } = string.Empty;

    [JsonPropertyName("touch")]
    public string Touch { get; set; } = string.Empty;

    [JsonPropertyName("taste")]
    public string Taste { get; set; } = string.Empty;
}

public class PlayerStateToReflect
{
    [JsonPropertyName("physical_condition")]
    public string PhysicalCondition { get; set; } = string.Empty;

    [JsonPropertyName("stamina_level")]
    public string StaminaLevel { get; set; } = string.Empty;

    [JsonPropertyName("status_effects")]
    public string StatusEffects { get; set; } = string.Empty;

    [JsonPropertyName("emotional_state")]
    public string EmotionalState { get; set; } = string.Empty;

    [JsonPropertyName("narrative_instruction")]
    public string NarrativeInstruction { get; set; } = string.Empty;
}

public class NpcContext
{
    [JsonPropertyName("character_name")]
    public string CharacterName { get; set; } = string.Empty;

    [JsonPropertyName("disposition")]
    public string Disposition { get; set; } = string.Empty;

    [JsonPropertyName("own_goals")]
    public string OwnGoals { get; set; } = string.Empty;

    [JsonPropertyName("likely_actions")]
    public string LikelyActions { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string Note { get; set; } = "Writer's real-time emulation is authoritative - this is context only";
}

public class NarrativeGoal
{
    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    [JsonPropertyName("flexibility")]
    public string Flexibility { get; set; } = string.Empty; // high|medium|low

    [JsonPropertyName("if_npc_cooperates")]
    public string IfNpcCooperates { get; set; } = string.Empty;

    [JsonPropertyName("if_npc_resists")]
    public string IfNpcResists { get; set; } = string.Empty;

    [JsonPropertyName("if_npc_pursues_own_agenda")]
    public string IfNpcPursuesOwnAgenda { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string Note { get; set; } = "Character authenticity takes precedence - these are goals, not requirements";
}

public class NpcOwnAgenda
{
    [JsonPropertyName("character_name")]
    public string CharacterName { get; set; } = string.Empty;

    [JsonPropertyName("wants")]
    public string Wants { get; set; } = string.Empty;

    [JsonPropertyName("might_do")]
    public string MightDo { get; set; } = string.Empty;

    [JsonPropertyName("regardless_of_player")]
    public string RegardlessOfPlayer { get; set; } = string.Empty;
}

public class ConsequencesQueue
{
    [JsonPropertyName("immediate")]
    public List<ImmediateConsequence> Immediate { get; set; } = new();

    [JsonPropertyName("delayed")]
    public List<DelayedConsequence> Delayed { get; set; } = new();
}

public class ImmediateConsequence
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("effect")]
    public string Effect { get; set; } = string.Empty;
}

public class DelayedConsequence
{
    [JsonPropertyName("scenes_until_trigger")]
    public int ScenesUntilTrigger { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("effect")]
    public string Effect { get; set; } = string.Empty;
}

public class PacingCalibration
{
    [JsonPropertyName("recent_scene_types")]
    public List<string> RecentSceneTypes { get; set; } = new();

    [JsonPropertyName("recommendation")]
    public string Recommendation { get; set; } = string.Empty;

    [JsonPropertyName("tension_trajectory")]
    public string TensionTrajectory { get; set; } = string.Empty;

    [JsonPropertyName("user_pattern_observed")]
    public string UserPatternObserved { get; set; } = string.Empty;

    [JsonPropertyName("adjustment")]
    public string Adjustment { get; set; } = string.Empty;
}

public class ContinuityNotes
{
    [JsonPropertyName("promises_to_keep")]
    public List<string> PromisesToKeep { get; set; } = new();

    [JsonPropertyName("elements_to_reincorporate")]
    public List<ElementToReincorporate> ElementsToReincorporate { get; set; } = new();

    [JsonPropertyName("relationship_changes")]
    public List<RelationshipChange> RelationshipChanges { get; set; } = new();
}

public class ElementToReincorporate
{
    [JsonPropertyName("element")]
    public string Element { get; set; } = string.Empty;

    [JsonPropertyName("optimal_reintroduction")]
    public string OptimalReintroduction { get; set; } = null!;

    [JsonPropertyName("purpose")]
    public string Purpose { get; set; } = string.Empty;
}

public class RelationshipChange
{
    [JsonPropertyName("character")]
    public string Character { get; set; } = string.Empty;

    [JsonPropertyName("previous_standing")]
    public int PreviousStanding { get; set; } // -10 to 10

    [JsonPropertyName("new_standing")]
    public int NewStanding { get; set; } // -10 to 10

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class WorldEvolution
{
    [JsonPropertyName("time_progressed")]
    public string TimeProgressed { get; set; } = string.Empty;

    [JsonPropertyName("calendar_position")]
    public string CalendarPosition { get; set; } = string.Empty;

    [JsonPropertyName("weather_shift")]
    public string WeatherShift { get; set; } = string.Empty;

    [JsonPropertyName("background_events")]
    public List<string> BackgroundEvents { get; set; } = new();

    [JsonPropertyName("world_state_changes")]
    public List<WorldStateChange> WorldStateChanges { get; set; } = new();
}

public class WorldStateChange
{
    [JsonPropertyName("element")]
    public string Element { get; set; } = string.Empty;

    [JsonPropertyName("previous")]
    public string Previous { get; set; } = string.Empty;

    [JsonPropertyName("current")]
    public string Current { get; set; } = string.Empty;

    [JsonPropertyName("scenes_until_critical")]
    public int? ScenesUntilCritical { get; set; }
}

public class MetaNarrative
{
    [JsonPropertyName("detected_patterns")]
    public List<string> DetectedPatterns { get; set; } = new();

    [JsonPropertyName("subversion_opportunity")]
    public string SubversionOpportunity { get; set; } = string.Empty;

    [JsonPropertyName("genre_expectations_met")]
    public List<string> GenreExpectationsMet { get; set; } = new();

    [JsonPropertyName("genre_expectations_needed")]
    public List<string> GenreExpectationsNeeded { get; set; } = new();
}