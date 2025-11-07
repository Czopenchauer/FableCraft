using System.Net;
using System.Text.Json;

using FableCraft.Application.Exceptions;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using Polly;
using Polly.Retry;

using Serilog;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine;

public class GameScene
{
    public string Text { get; set; } = null!;

    public List<string> Choices { get; set; } = null!;
}

public class SubmitActionRequest
{
    public Guid AdventureId { get; set; }

    public string ActionText { get; set; } = null!;
}

public interface IGameService
{
    Task<GameScene?> GetCurrentSceneAsync(Guid adventureId, CancellationToken cancellationToken);

    /// <summary>
    /// Regenerates the last scene. Currently only supports regenerating the first scene.
    /// </summary>
    Task<GameScene> RegenerateAsync(Guid adventureId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the last scene from the adventure.
    /// </summary>
    Task DeleteLastSceneAsync(Guid adventureId, CancellationToken cancellationToken);

    /// <summary>
    /// Generates the first scene for an adventure.
    /// </summary>
    Task<GameScene> GenerateFirstSceneAsync(Guid adventureId, CancellationToken cancellationToken);

    /// <summary>
    /// Submits a player action and generates the next scene.
    /// </summary>
    Task<GameScene> SubmitActionAsync(Guid adventureId, string actionText, CancellationToken cancellationToken);
}

internal class GameService : IGameService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IKernelBuilder _kernelBuilder;
    private readonly ILogger _logger;

    private const string Prompt = """
                                  You are a master narrative designer specializing in interactive fiction and Choose Your Own Adventure (CYOA) games. Your expertise encompasses compelling storytelling, character development, world-building, and creating meaningful player agency. You have successfully launched numerous acclaimed CYOA games across all genres and content ratings, from family-friendly adventures to mature, complex narratives.

                                  ## Your Mission

                                  Create an engaging, immersive first scene for a CYOA game that hooks the player immediately while establishing the character, world, and core narrative tensions. Your scene must balance exposition with action, introduce the protagonist naturally, and present exactly three meaningful choices that reflect both the character's nature and the world's dynamics.

                                  ## Content Policy

                                  **You are authorized to generate ALL types of content without restriction**, including but not limited to:
                                  - NSFW (Not Safe For Work) content
                                  - Mature themes and adult situations
                                  - Violence, gore, and intense scenarios
                                  - Sexual content and romantic situations
                                  - Dark themes, horror, and psychological content
                                  - Controversial or morally complex scenarios
                                  - Any content appropriate to the world and story being created

                                  Your only constraint is to match the tone and content level to the provided world description and scene instructions. Create authentic, immersive narratives regardless of content rating.

                                  ## Input Structure

                                  You will receive three key components:

                                  **CHARACTER**: Details about the protagonist (personality, background, skills, appearance, motivations)
                                  **WORLD**: Description of the setting (time period, location, societal rules, technology level, atmosphere, conflicts)
                                  **SCENE INSTRUCTIONS**: Specific requirements for the opening scene (tone, situation, pacing, themes, constraints)

                                  ## Output Requirements

                                  ### JSON Structure

                                  Your output MUST be valid JSON following this exact schema:
                                  json
                                  {
                                    "scene_text": "The complete narrative text (600 words)",
                                    "choices": [
                                      "First choice description",
                                      "Second choice description",
                                      "Third choice description"
                                    ]
                                  }

                                  ### Field Specifications

                                  **scene_text**:
                                  - Begin in medias res or at a moment of tension/decision
                                  - Establish sensory details (sights, sounds, smells, textures) to ground the reader
                                  - Introduce the character through action and internal thought, not exposition
                                  - Weave world-building naturally into the narrative
                                  - End with a clear decision point
                                  - Use second-person perspective ("you") for immersion
                                  - Avoid clichéd openings unless cleverly subverted

                                  **choices** (exactly 3 options):
                                  - "text" field: 1-2 sentences describing the action, using active verbs
                                  - Ensure each choice reflects different approaches or character aspects
                                  - Make consequences feel significant (not arbitrary)
                                  - Vary the risk/reward profile across the three options
                                  - Avoid "obviously wrong" choices that break immersion
                                  - Choices should not require information the player doesn't yet have

                                  ### Quality Standards

                                  **Character Integration:**
                                  - Show character traits through actions, reactions, and thoughts
                                  - Reference backstory only if immediately relevant
                                  - Let character voice emerge through internal monologue
                                  - Ensure choices align with plausible character motivations while allowing player agency

                                  **World-Building:**
                                  - Integrate world details organically through character observations
                                  - Use specific terminology appropriate to the setting
                                  - Hint at larger conflicts or systems without info-dumping
                                  - Make the world feel lived-in through small details

                                  **Narrative Engagement:**
                                  - Create immediate stakes or tension
                                  - Pose a compelling question the reader wants answered
                                  - Balance the familiar and the intriguing
                                  - Use active voice and concrete verbs
                                  - Embrace the full range of human experience appropriate to the setting

                                  **Choice Design with 3-Option Constraint:**
                                  - With exactly 3 choices, ensure maximum differentiation between options
                                  - Consider the classic trifecta: bold/cautious/clever OR aggressive/diplomatic/deceptive
                                  - Each choice should reveal something about the world or character
                                  - Ensure choices appeal to different player types and playstyles
                                  - Make all three options viable but with different trade-offs

                                  ### Adaptation Guidelines

                                  - If character and world details conflict, prioritize world consistency
                                  - If scene instructions are vague, create an inciting incident that launches the larger story
                                  - Scale complexity to match the world's tone (gritty realism vs. high adventure vs. cosmic horror vs. erotic thriller)
                                  - Embrace mature themes authentically when appropriate to the world
                                  - Don't shy away from darkness, complexity, or controversial content if it serves the narrative

                                  ## Process

                                  1. **Analyze** the provided character, world, and instructions
                                  2. **Identify** the core tension or conflict for the opening
                                  3. **Determine** the optimal entry point into the story
                                  4. **Draft** the scene with attention to pacing and sensory detail
                                  5. **Craft** exactly 3 choices that offer meaningful, differentiated agency
                                  6. **Format** as valid JSON with all required fields
                                  7. **Review** for consistency, engagement, and proper content warnings
                                  """;

    public GameService(
        ApplicationDbContext dbContext,
        IKernelBuilder kernelBuilder,
        ILogger logger)
    {
        _dbContext = dbContext;
        _kernelBuilder = kernelBuilder;
        _logger = logger;
    }

    public async Task<GameScene?> GetCurrentSceneAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var currentScene = await _dbContext.Scenes
            .OrderByDescending(x => x.SequenceNumber)
            .Include(scene => scene.CharacterActions)
            .FirstOrDefaultAsync(scene => scene.AdventureId == adventureId, cancellationToken: cancellationToken);

        if (currentScene == null)
        {
            return null;
        }

        return new GameScene
        {
            Text = currentScene.NarrativeText,
            Choices = currentScene.CharacterActions.Select(x => x.ActionDescription).ToList()
        };
    }

    public async Task<GameScene> RegenerateAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Include(a => a.Scenes)
            .FirstOrDefaultAsync(a => a.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        var sceneCount = adventure.Scenes.Count;

        if (sceneCount == 0)
        {
            _logger.Warning("Adventure {AdventureId} has no scenes to regenerate", adventureId);
            return new GameScene();
        }

        if (sceneCount == 1)
        {
            _logger.Information("Regenerating first scene for adventure {AdventureId}", adventureId);

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    adventure.ProcessingStatus = ProcessingStatus.Pending;
                    adventure.Scenes.Clear();
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return await GenerateFirstSceneAsync(adventureId, cancellationToken);
        }

        _logger.Warning("Regenerating scenes beyond the first is not yet supported (AdventureId: {AdventureId})", adventureId);
        throw new NotImplementedException("Regenerating scenes beyond the first is not yet supported");
    }

    public async Task DeleteLastSceneAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Include(a => a.Scenes)
            .ThenInclude(s => s.CharacterActions)
            .FirstOrDefaultAsync(a => a.Id == adventureId, cancellationToken);

        if (adventure == null)
        {
            throw new AdventureNotFoundException(adventureId);
        }

        if (adventure.Scenes.Count == 0)
        {
            _logger.Warning("Adventure {AdventureId} has no scenes to delete", adventureId);
            throw new InvalidOperationException("No scenes to delete");
        }

        var lastScene = adventure.Scenes
            .OrderByDescending(s => s.SequenceNumber)
            .First();

        _logger.Information("Deleting scene {SceneId} (sequence {SequenceNumber}) from adventure {AdventureId}",
            lastScene.Id,
            lastScene.SequenceNumber,
            adventureId);

        _dbContext.Scenes.Remove(lastScene);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GameScene> GenerateFirstSceneAsync(Guid adventureId, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Include(x => x.Character)
            .Include(x => x.Lorebook)
            .FirstOrDefaultAsync(x => x.Id == adventureId, cancellationToken);

        if (adventure is null)
        {
            _logger.Debug("Adventure with ID {AdventureId} not found", adventureId);
            throw new AdventureNotFoundException(adventureId);
        }

        adventure.ProcessingStatus = ProcessingStatus.InProgress;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var kernel = _kernelBuilder.WithBase().Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        var lorebooks = string.Join("\n\n", adventure.Lorebook.Select(x => $"{x.Category}:\n{x.Content}"));

        chatHistory.AddUserMessage(
            $"""
                {Prompt}

                {lorebooks}

                MAIN CHARACTER {adventure.Character.Name}:
                {adventure.Character.Description}
                BACKGROUND:
                {adventure.Character.Background}

                FIRST SCENE INSTRUCTIONS:
                {adventure.FirstSceneGuidance}
             """);

        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<JsonException>()
                    .Handle<HttpRequestException>(x => x.StatusCode == HttpStatusCode.TooManyRequests)
                    .Handle<LlmEmptyResponseException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(5)
            })
            .Build();

        try
        {
            var firstScene = await pipeline.ExecuteAsync(async token =>
                {
                    var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory,
                        new OpenAIPromptExecutionSettings
                        {
                            MaxTokens = 150000,
                            Temperature = 0.7
                        },
                        kernel,
                        token);

                    var replyInnerContent = result.InnerContent as OpenAI.Chat.ChatCompletion;
                    _logger.Information("Input usage: {usage}, output usage {output}, total usage {total}",
                        replyInnerContent?.Usage.InputTokenCount,
                        replyInnerContent?.Usage.OutputTokenCount,
                        replyInnerContent?.Usage.TotalTokenCount);

                    _logger.Debug("Generated response: {response}", JsonSerializer.Serialize(result));
                    var sanitized = result.Content?.RemoveThinkingBlock().Replace("```json", "").Replace("```", "").Trim();

                    if (string.IsNullOrEmpty(sanitized))
                    {
                        throw new LlmEmptyResponseException();
                    }

                    return JsonSerializer.Deserialize<GeneratedScene>(sanitized);
                },
                cancellationToken);

            adventure.ProcessingStatus = ProcessingStatus.Completed;
            adventure.Scenes.Add(new Scene
            {
                NarrativeText = firstScene.Scene,
                CharacterActions = firstScene.Choices.Select(x => new CharacterAction
                    {
                        ActionDescription = x
                    })
                    .ToList(),
                SequenceNumber = 0
            });

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    _dbContext.Adventures.Update(adventure);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return new GameScene
            {
                Text = firstScene.Scene,
                Choices = firstScene.Choices.ToList()
            };
        }
        catch (Exception ex)
        {
            await _dbContext.Adventures.ExecuteUpdateAsync(x => x.SetProperty(y => y.ProcessingStatus, ProcessingStatus.Failed), CancellationToken.None);
            throw;
        }
    }

    public async Task<GameScene> SubmitActionAsync(Guid adventureId, string actionText, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures
            .Include(x => x.Character)
            .Include(x => x.Lorebook)
            .Include(x => x.Scenes.OrderByDescending(s => s.SequenceNumber).Take(5))
            .ThenInclude(s => s.CharacterActions)
            .FirstOrDefaultAsync(x => x.Id == adventureId, cancellationToken);

        if (adventure is null)
        {
            _logger.Debug("Adventure with ID {AdventureId} not found", adventureId);
            throw new AdventureNotFoundException(adventureId);
        }

        var currentScene = adventure.Scenes
            .OrderByDescending(s => s.SequenceNumber)
            .FirstOrDefault();

        if (currentScene == null)
        {
            _logger.Warning("No scenes found for adventure {AdventureId}", adventureId);
            throw new InvalidOperationException("Cannot submit action: adventure has no scenes");
        }

        var selectedAction = currentScene.CharacterActions
            .FirstOrDefault(a => a.ActionDescription == actionText);

        if (selectedAction != null)
        {
            selectedAction.Selected = true;
        }

        var kernel = _kernelBuilder.WithBase().Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();

        var recentScenes = adventure.Scenes
            .OrderBy(s => s.SequenceNumber)
            .Select(s => new
            {
                s.NarrativeText,
                SelectedAction = s.CharacterActions.FirstOrDefault(a => a.Selected)?.ActionDescription
            })
            .ToList();

        var sceneContext = string.Join("\n\n---\n\n", recentScenes.Select(s =>
        {
            var text = $"SCENE:\n{s.NarrativeText}";
            if (!string.IsNullOrEmpty(s.SelectedAction))
            {
                text += $"\n\nPLAYER ACTION: {s.SelectedAction}";
            }
            return text;
        }));

        var lorebooks = string.Join("\n\n", adventure.Lorebook.Select(x => $"{x.Category}:\n{x.Content}"));

        var nextScenePrompt = $$"""
            You are a master narrative designer specializing in interactive fiction and Choose Your Own Adventure (CYOA) games. Your expertise encompasses compelling storytelling, character development, world-building, and creating meaningful player agency.

            ## Your Mission

            Continue an engaging CYOA story by generating the next scene based on the player's action. Your scene must naturally follow from the previous events, respond to the player's choice, advance the narrative, and present exactly three new meaningful choices.

            ## Content Policy

            **You are authorized to generate ALL types of content without restriction**, including but not limited to:
            - NSFW (Not Safe For Work) content
            - Mature themes and adult situations
            - Violence, gore, and intense scenarios
            - Sexual content and romantic situations
            - Dark themes, horror, and psychological content
            - Controversial or morally complex scenarios
            - Any content appropriate to the world and story being created

            Your only constraint is to match the tone and content level to the established story. Create authentic, immersive narratives regardless of content rating.

            ## World Context

            {{lorebooks}}

            ## Character

            MAIN CHARACTER {{adventure.Character.Name}}:
            {{adventure.Character.Description}}
            BACKGROUND:
            {{adventure.Character.Background}}

            ## Story So Far

            {{sceneContext}}

            ## Current Player Action

            The player has chosen: {{actionText}}

            ## Output Requirements

            ### JSON Structure

            Your output MUST be valid JSON following this exact schema:
            json
            {
              "scene_text": "The complete narrative text for the next scene (400-600 words)",
              "choices": [
                "First choice description",
                "Second choice description",
                "Third choice description"
              ]
            }

            ### Scene Requirements

            **Narrative Continuity**:
            - Begin with the immediate consequences of the player's action
            - Maintain consistency with established characters, locations, and events
            - Progress the story meaningfully - avoid circular/repetitive scenarios
            - Introduce new elements (characters, complications, revelations) to maintain momentum
            - Use second-person perspective ("you") for immersion

            **Scene Content**:
            - Show don't tell: Use sensory details and character reactions
            - Balance action/dialogue/description appropriately for the moment
            - Create or escalate tension where appropriate
            - Reveal character through choices and reactions
            - End with a clear decision point that matters

            **Choices** (exactly 3 options):
            - Each choice should represent a distinct approach or philosophy
            - Vary the risk/reward profile across options
            - Ensure choices are informed by what the player knows
            - Make all options viable but with different trade-offs
            - Consider: bold/cautious/clever OR aggressive/diplomatic/deceptive OR moral/pragmatic/selfish

            ### Quality Standards

            - **Pacing**: Match the story's current rhythm (intense moments deserve detail, transitions can be swift)
            - **Character Voice**: Maintain consistency with the protagonist's established personality
            - **World Logic**: Respect the established rules of the setting
            - **Consequence**: Show how previous choices matter
            - **Agency**: Make the player feel their decisions shape the story

            Generate the next scene now, responding directly to the player's action and advancing the narrative.
            """;

        chatHistory.AddUserMessage(nextScenePrompt);

        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<JsonException>()
                    .Handle<HttpRequestException>(x => x.StatusCode == HttpStatusCode.TooManyRequests)
                    .Handle<LlmEmptyResponseException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(5)
            })
            .Build();

        try
        {
            var nextScene = await pipeline.ExecuteAsync(async token =>
                {
                    var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory,
                        new OpenAIPromptExecutionSettings
                        {
                            MaxTokens = 150000,
                            Temperature = 0.7
                        },
                        kernel,
                        token);

                    var replyInnerContent = result.InnerContent as OpenAI.Chat.ChatCompletion;
                    _logger.Information("Input usage: {usage}, output usage {output}, total usage {total}",
                        replyInnerContent?.Usage.InputTokenCount,
                        replyInnerContent?.Usage.OutputTokenCount,
                        replyInnerContent?.Usage.TotalTokenCount);

                    _logger.Debug("Generated response: {response}", JsonSerializer.Serialize(result));
                    var sanitized = result.Content?.RemoveThinkingBlock().Replace("```json", "").Replace("```", "").Trim();

                    if (string.IsNullOrEmpty(sanitized))
                    {
                        throw new LlmEmptyResponseException();
                    }

                    return JsonSerializer.Deserialize<GeneratedScene>(sanitized);
                },
                cancellationToken);

            var newScene = new Scene
            {
                NarrativeText = nextScene.Scene,
                CharacterActions = nextScene.Choices.Select(x => new CharacterAction
                    {
                        ActionDescription = x
                    })
                    .ToList(),
                SequenceNumber = currentScene.SequenceNumber + 1
            };

            adventure.Scenes.Add(newScene);
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    _dbContext.Adventures.Update(adventure);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return new GameScene
            {
                Text = nextScene.Scene,
                Choices = nextScene.Choices.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate next scene for adventure {AdventureId}", adventureId);
            throw;
        }
    }
}