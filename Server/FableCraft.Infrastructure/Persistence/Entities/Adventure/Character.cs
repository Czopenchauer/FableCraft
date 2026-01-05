namespace FableCraft.Infrastructure.Persistence.Entities.Adventure;

public readonly struct CharacterImportance : IEquatable<CharacterImportance>
{
    public readonly static CharacterImportance ArcImportance = new("arc_important");
    public readonly static CharacterImportance Significant = new("significant");
    public readonly static CharacterImportance Background = new("background");

    public string Value { get; }

    private CharacterImportance(string value) => Value = value;

    public override string ToString() => Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override bool Equals(object? obj) => obj is CharacterImportance other && Equals(other);

    public bool Equals(CharacterImportance other) => Value == other.Value;

    public static bool operator ==(CharacterImportance left, CharacterImportance right) => left.Equals(right);

    public static bool operator !=(CharacterImportance left, CharacterImportance right) => !left.Equals(right);

    public static implicit operator string(CharacterImportance characterImportance) => characterImportance.Value;
}

public sealed class Character : IEntity
{
    public Guid Id { get; set; }

    public Guid AdventureId { get; set; }

    public required string Name { get; set; }

    public required CharacterImportance Importance { get; set; }

    public int Version { get; set; }

    public List<CharacterState> CharacterStates { get; set; } = new();

    public List<CharacterRelationship> CharacterRelationships { get; set; } = new();

    public List<CharacterSceneRewrite> CharacterSceneRewrites { get; set; } = new();

    public List<CharacterMemory> CharacterMemories { get; set; } = new();
}