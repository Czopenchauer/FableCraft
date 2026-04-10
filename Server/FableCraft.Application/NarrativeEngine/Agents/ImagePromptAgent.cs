using FableCraft.Application.NarrativeEngine.Agents.Builders;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

namespace FableCraft.Application.NarrativeEngine.Agents;

/// <summary>
/// Generates Stable Diffusion optimized prompts for scene image generation.
/// </summary>
internal sealed class ImagePromptAgent
{
    private readonly IAgentKernel _agentKernel;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly KernelBuilderFactory _kernelBuilderFactory;
    private readonly ILogger _logger;

    public ImagePromptAgent(
        IAgentKernel agentKernel,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        KernelBuilderFactory kernelBuilderFactory,
        ILogger logger)
    {
        _agentKernel = agentKernel;
        _dbContextFactory = dbContextFactory;
        _kernelBuilderFactory = kernelBuilderFactory;
        _logger = logger;
    }

    /// <summary>
    /// Generates image prompts for a scene.
    /// </summary>
    public async Task<ImagePromptOutput> InvokeAsync(
        ImagePromptInput input,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Get LLM preset for this agent
        var agentPreset = await dbContext.AdventureAgentLlmPresets
            .Include(x => x.LlmPreset)
            .FirstOrDefaultAsync(x =>
                    x.AdventureId == input.AdventureId &&
                    x.AgentName == AgentName.ImagePromptAgent,
                cancellationToken);

        LlmPreset llmPreset;
        if (agentPreset != null)
        {
            llmPreset = agentPreset.LlmPreset;
        }
        else
        {
            // Fall back to default preset
            llmPreset = await dbContext.LlmPresets.FirstAsync(cancellationToken);
        }

        var kernelBuilder = _kernelBuilderFactory.Create(llmPreset);
        var systemPrompt = await GetPromptAsync(input.PromptPath);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);

        var contextPrompt = BuildContextPrompt(input);
        chatHistory.AddUserMessage(contextPrompt);

        chatHistory.AddUserMessage("Generate the image prompts for this scene.");

        var outputParser = ResponseParser.CreateJsonParser<ImagePromptOutput>("image_prompt", true);
        var promptExecutionSettings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();
        var kernel = kernelBuilder.Create().Build();

        var output = await _agentKernel.SendRequestAsync(
            chatHistory,
            outputParser,
            promptExecutionSettings,
            nameof(ImagePromptAgent),
            kernel,
            cancellationToken);

        _logger.Information(
            "Generated image prompts for scene {SceneId}: positive={PositiveLength}, negative={NegativeLength}",
            input.SceneId,
            output.PositivePrompt?.Length ?? 0,
            output.NegativePrompt?.Length ?? 0);

        return output;
    }

    private static async Task<string> GetPromptAsync(string promptPath)
    {
        var agentPromptPath = Path.Combine(promptPath, "ImagePromptAgent.md");
        if (!File.Exists(agentPromptPath))
        {
            throw new FileNotFoundException($"ImagePromptAgent prompt not found: {agentPromptPath}");
        }
        return await File.ReadAllTextAsync(agentPromptPath);
    }

    private static string BuildContextPrompt(ImagePromptInput input)
    {
        var sceneTrackerSection = "";
        if (input.SceneTracker != null)
        {
            sceneTrackerSection = $"""
                <scene_tracker>
                Time: {input.SceneTracker.Time}
                Location: {input.SceneTracker.Location}
                Weather: {input.SceneTracker.Weather}
                Characters Present: {string.Join(", ", input.SceneTracker.CharactersPresent)}
                </scene_tracker>
                """;
        }

        var mainCharacterSection = "";
        if (!string.IsNullOrEmpty(input.MainCharacterName) || !string.IsNullOrEmpty(input.MainCharacterAppearance))
        {
            mainCharacterSection = $"""
                <main_character>
                Name: {input.MainCharacterName ?? "Unknown"}
                Appearance: {input.MainCharacterAppearance ?? "Not specified"}
                </main_character>
                """;
        }

        var genreSection = !string.IsNullOrEmpty(input.Genre)
            ? $"<genre>{input.Genre}</genre>"
            : "";

        return $"""
                <scene_narrative>
                {input.NarrativeText}
                </scene_narrative>

                {sceneTrackerSection}

                {mainCharacterSection}

                {genreSection}
                """;
    }
}

/// <summary>
/// Input for image prompt generation.
/// </summary>
public sealed class ImagePromptInput
{
    public required Guid AdventureId { get; init; }
    public required Guid SceneId { get; init; }
    public required string PromptPath { get; init; }
    public required string NarrativeText { get; init; }
    public SceneTracker? SceneTracker { get; init; }
    public string? MainCharacterName { get; init; }
    public string? MainCharacterAppearance { get; init; }
    public string? Genre { get; init; }
}

/// <summary>
/// Output from image prompt generation.
/// </summary>
public sealed class ImagePromptOutput
{
    /// <summary>
    /// The positive prompt describing what to generate.
    /// Optimized for Stable Diffusion.
    /// </summary>
    public required string PositivePrompt { get; init; }

    /// <summary>
    /// The negative prompt describing what to avoid.
    /// </summary>
    public string? NegativePrompt { get; init; }
}
