namespace FableCraft.Application.Model;

public class LlmLogResponseDto
{
    public required Guid Id { get; init; }

    public Guid? AdventureId { get; init; }

    public Guid? SceneId { get; init; }

    public string? CallerName { get; init; }

    public required string RequestContent { get; init; }

    public required string ResponseContent { get; init; }

    public required DateTimeOffset ReceivedAt { get; init; }

    public int? InputToken { get; init; }

    public int? OutputToken { get; init; }

    public int? TotalToken { get; init; }

    public required long Duration { get; init; }
}

public class LlmLogListResponseDto
{
    public required List<LlmLogResponseDto> Items { get; init; }

    public required int TotalCount { get; init; }
}
