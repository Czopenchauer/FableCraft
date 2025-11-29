using System.Text.Json.Serialization;

using NpgsqlTypes;

namespace FableCraft.Infrastructure.Persistence.Entities;

public sealed class CharacterState
{
    public Guid Id { get; set; }

    public Guid CharacterId { get; set; }

    public Character Character { get; set; } = null!;

    public Guid SceneId { get; set; }

    public int SequenceNumber { get; set; }

    public required string Description { get; set; }

    public CharacterStats CharacterStats { get; init; } = null!;

    public CharacterTracker Tracker { get; init; } = null!;
}

public class CharacterStats
{
    [JsonPropertyName("character_identity")]
    public required CharacterIdentity CharacterIdentity { get; set; }

    [JsonPropertyName("personality")]
    public Personality? Personality { get; set; }

    [JsonPropertyName("goals_and_motivations")]
    public GoalsAndMotivations? GoalsAndMotivations { get; set; }

    [JsonPropertyName("knowledge_and_beliefs")]
    public KnowledgeAndBeliefs? KnowledgeAndBeliefs { get; set; }

    [JsonPropertyName("relationships")]
    public Relationships? Relationships { get; set; }

    [JsonPropertyName("memory_stream")]
    public List<MemoryEntry>? MemoryStream { get; set; }

    [JsonPropertyName("emotional_state")]
    public EmotionalState? EmotionalState { get; set; }

    [JsonPropertyName("character_arc")]
    public CharacterArc? CharacterArc { get; set; }

    [JsonPropertyName("behavioral_state")]
    public BehavioralState? BehavioralState { get; set; }

    [JsonPropertyName("integration")]
    public Integration? Integration { get; set; }
}

public class CharacterIdentity
{
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }

    [JsonPropertyName("archetype")]
    public string? Archetype { get; set; }
}

public class Personality
{
    [JsonPropertyName("five_factor_model")]
    public FiveFactorModel? FiveFactorModel { get; set; }

    [JsonPropertyName("core_traits")]
    public List<string>? CoreTraits { get; set; }

    [JsonPropertyName("speech_patterns")]
    public SpeechPatterns? SpeechPatterns { get; set; }

    [JsonPropertyName("moral_alignment")]
    public MoralAlignment? MoralAlignment { get; set; }
}

public class FiveFactorModel
{
    [JsonPropertyName("openness")]
    public double Openness { get; set; }

    [JsonPropertyName("conscientiousness")]
    public double Conscientiousness { get; set; }

    [JsonPropertyName("extraversion")]
    public double Extraversion { get; set; }

    [JsonPropertyName("agreeableness")]
    public double Agreeableness { get; set; }

    [JsonPropertyName("neuroticism")]
    public double Neuroticism { get; set; }
}

public class SpeechPatterns
{
    [JsonPropertyName("formality_level")]
    public string? FormalityLevel { get; set; }


    [JsonPropertyName("accent_or_dialect")]
    public string? AccentOrDialect { get; set; }
}

public class MoralAlignment
{
    [JsonPropertyName("lawful_chaotic_axis")]
    public double LawfulChaoticAxis { get; set; }

    [JsonPropertyName("good_evil_axis")]
    public double GoodEvilAxis { get; set; }
}

public class GoalsAndMotivations
{
    [JsonPropertyName("primary_goal")]
    public PrimaryGoal? PrimaryGoal { get; set; }

    [JsonPropertyName("secondary_goals")]
    public List<SecondaryGoal>? SecondaryGoals { get; set; }

    [JsonPropertyName("motivations")]
    public Motivations? Motivations { get; set; }
}

public class PrimaryGoal
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("goal_type")]
    public string? GoalType { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("time_sensitivity")]
    public string? TimeSensitivity { get; set; }

    [JsonPropertyName("progress_percentage")]
    public int ProgressPercentage { get; set; }

    [JsonPropertyName("success_conditions")]
    public List<string>? SuccessConditions { get; set; }

    [JsonPropertyName("failure_conditions")]
    public List<string>? FailureConditions { get; set; }
}

public class SecondaryGoal
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("goal_type")]
    public string? GoalType { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("prerequisites")]
    public List<string>? Prerequisites { get; set; }
}

public class Motivations
{
    [JsonPropertyName("intrinsic")]
    public List<string>? Intrinsic { get; set; }

    [JsonPropertyName("extrinsic")]
    public List<string>? Extrinsic { get; set; }
}

public class KnowledgeAndBeliefs
{
    [JsonPropertyName("world_knowledge")]
    public List<WorldKnowledge>? WorldKnowledge { get; set; }

    [JsonPropertyName("beliefs_about_protagonist")]
    public List<BeliefAboutProtagonist>? BeliefsAboutProtagonist { get; set; }

    [JsonPropertyName("secrets_held")]
    public List<Secret>? SecretsHeld { get; set; }
}

public class WorldKnowledge
{
    [JsonPropertyName("fact")]
    public string? Fact { get; set; }

    [JsonPropertyName("confidence_level")]
    public double ConfidenceLevel { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("learned_at_scene")]
    public string? LearnedAtScene { get; set; }

    [JsonPropertyName("kg_reference")]
    public string? KgReference { get; set; }
}

public class BeliefAboutProtagonist
{
    [JsonPropertyName("belief")]
    public string? Belief { get; set; }

    [JsonPropertyName("confidence_level")]
    public double ConfidenceLevel { get; set; }

    [JsonPropertyName("evidence")]
    public List<string>? Evidence { get; set; }

    [JsonPropertyName("formed_at_scene")]
    public string? FormedAtScene { get; set; }
}

public class Secret
{
    [JsonPropertyName("secret_content")]
    public string? SecretContent { get; set; }

    [JsonPropertyName("willingness_to_share")]
    public double WillingnessToShare { get; set; }

    [JsonPropertyName("reveal_conditions")]
    public List<string>? RevealConditions { get; set; }
}

public class Relationships
{
    [JsonPropertyName("with_protagonist")]
    public RelationshipWithProtagonist? WithProtagonist { get; set; }

    [JsonPropertyName("with_other_characters")]
    public List<RelationshipWithOther>? WithOtherCharacters { get; set; }

    [JsonPropertyName("faction_affiliations")]
    public List<FactionAffiliation>? FactionAffiliations { get; set; }
}

public class RelationshipWithProtagonist
{
    [JsonPropertyName("relationship_type")]
    public string? RelationshipType { get; set; }

    [JsonPropertyName("trust_level")]
    public int TrustLevel { get; set; }

    [JsonPropertyName("affection_level")]
    public int AffectionLevel { get; set; }

    [JsonPropertyName("respect_level")]
    public int RespectLevel { get; set; }

    [JsonPropertyName("relationship_tags")]
    public List<string>? RelationshipTags { get; set; }

    [JsonPropertyName("first_met_scene")]
    public string? FirstMetScene { get; set; }

    [JsonPropertyName("reputation_influence")]
    public string? ReputationInfluence { get; set; }

    [JsonPropertyName("shared_experiences")]
    public List<SharedExperience>? SharedExperiences { get; set; }

    [JsonPropertyName("promises_made")]
    public List<Promise>? PromisesMade { get; set; }

    [JsonPropertyName("debts_and_obligations")]
    public List<DebtAndObligation>? DebtsAndObligations { get; set; }
}

public class DebtAndObligation
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("direction")]
    public string? Direction { get; set; }

    [JsonPropertyName("magnitude")]
    public string? Magnitude { get; set; }

    [JsonPropertyName("origin_event")]
    public string? OriginEvent { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("terms_of_repayment")]
    public string? TermsOfRepayment { get; set; }

    [JsonPropertyName("urgency")]
    public string? Urgency { get; set; }
}

public class SharedExperience
{
    [JsonPropertyName("scene_reference")]
    public string? SceneReference { get; set; }

    [JsonPropertyName("experience_type")]
    public string? ExperienceType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("emotional_impact")]
    public string? EmotionalImpact { get; set; }

    [JsonPropertyName("trust_change")]
    public int TrustChange { get; set; }
}

public class Promise
{
    [JsonPropertyName("promise")]
    public string? PromiseText { get; set; }

    [JsonPropertyName("scene_made")]
    public string? SceneMade { get; set; }

    [JsonPropertyName("is_fulfilled")]
    public bool IsFulfilled { get; set; }
}

public class RelationshipWithOther
{
    [JsonPropertyName("character_reference")]
    public string? CharacterReference { get; set; }

    [JsonPropertyName("relationship_type")]
    public string? RelationshipType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("trust_level")]
    public int TrustLevel { get; set; }

    [JsonPropertyName("current_status")]
    public string? CurrentStatus { get; set; }

    [JsonPropertyName("conflict_reason")]
    public string? ConflictReason { get; set; }
}

public class FactionAffiliation
{
    [JsonPropertyName("faction_name")]
    public string? FactionName { get; set; }

    [JsonPropertyName("standing")]
    public int Standing { get; set; }

    [JsonPropertyName("rank_or_role")]
    public string? RankOrRole { get; set; }
}

public class MemoryEntry
{
    [JsonPropertyName("scene_reference")]
    public string? SceneReference { get; set; }

    [JsonPropertyName("memory_type")]
    public string? MemoryType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("emotional_valence")]
    public string? EmotionalValence { get; set; }

    [JsonPropertyName("participants")]
    public List<string>? Participants { get; set; }

    [JsonPropertyName("outcomes")]
    public List<string>? Outcomes { get; set; }

    [JsonPropertyName("event_reference")]
    public string? EventReference { get; set; }
}

public class EmotionalState
{
    [JsonPropertyName("current_emotions")]
    public CurrentEmotions? CurrentEmotions { get; set; }

    [JsonPropertyName("emotional_triggers")]
    public EmotionalTriggers? EmotionalTriggers { get; set; }
}

public class CurrentEmotions
{
    [JsonPropertyName("primary_emotion")]
    public string? PrimaryEmotion { get; set; }

    [JsonPropertyName("secondary_emotions")]
    public List<string>? SecondaryEmotions { get; set; }

    [JsonPropertyName("intensity")]
    public double Intensity { get; set; }
}

public class EmotionalTriggers
{
    [JsonPropertyName("positive")]
    public List<string>? Positive { get; set; }

    [JsonPropertyName("negative")]
    public List<string>? Negative { get; set; }
}

public class CharacterArc
{
    [JsonPropertyName("arc_type")]
    public string? ArcType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("current_stage")]
    public string? CurrentStage { get; set; }

    [JsonPropertyName("arc_stages")]
    public List<ArcStage>? ArcStages { get; set; }

    [JsonPropertyName("key_decisions_pending")]
    public List<string>? KeyDecisionsPending { get; set; }
}

public class ArcStage
{
    [JsonPropertyName("stage_name")]
    public string? StageName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("key_events")]
    public List<string>? KeyEvents { get; set; }

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }

    [JsonPropertyName("progress_percentage")]
    public int? ProgressPercentage { get; set; }
}

public class BehavioralState
{
    [JsonPropertyName("current_plan")]
    public CurrentPlan? CurrentPlan { get; set; }

    [JsonPropertyName("action_tendencies")]
    public ActionTendencies? ActionTendencies { get; set; }
}

public class CurrentPlan
{
    [JsonPropertyName("intention")]
    public string? Intention { get; set; }

    [JsonPropertyName("steps")]
    public List<string>? Steps { get; set; }

    [JsonPropertyName("expected_duration_scenes")]
    public string ExpectedDurationScenes { get; set; } = string.Empty;

    [JsonPropertyName("contingency_plans")]
    public Dictionary<string, string>? ContingencyPlans { get; set; }
}

public class ActionTendencies
{
    [JsonPropertyName("default_response_to_aggression")]
    public string? DefaultResponseToAggression { get; set; }

    [JsonPropertyName("response_to_deception")]
    public string? ResponseToDeception { get; set; }

    [JsonPropertyName("response_to_kindness")]
    public string? ResponseToKindness { get; set; }
}


public class Integration
{
    [JsonPropertyName("relevant_lore")]
    public List<string>? RelevantLore { get; set; }

    [JsonPropertyName("recent_events_aware_of")]
    public List<string>? RecentEventsAwareOf { get; set; }

    [JsonPropertyName("location_knowledge")]
    public List<string>? LocationKnowledge { get; set; }

    [JsonPropertyName("cultural_background")]
    public string? CulturalBackground { get; set; }
}