using System.Text;

using FableCraft.Application.Model;
using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Application.NarrativeEngine.Plugins.Impl;
using FableCraft.Infrastructure;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.Chat;

public interface IChatService
{
    Task<IEnumerable<ChatSessionResponseDto>> GetAllSessionsAsync(CancellationToken cancellationToken);

    Task<ChatSessionWithMessagesDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<ChatSessionResponseDto> CreateSessionAsync(ChatSessionDto dto, CancellationToken cancellationToken);

    Task<ChatSessionResponseDto?> UpdatePresetAsync(Guid sessionId, Guid llmPresetId, CancellationToken cancellationToken);

    Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<bool> DeleteLatestMessageAsync(Guid sessionId, CancellationToken cancellationToken);

    IAsyncEnumerable<ChatSseChunk> StreamMessageAsync(Guid sessionId, string userMessage, CancellationToken cancellationToken);
}

public class ChatSessionWithMessagesDto
{
    public required Guid Id { get; init; }

    public required Guid AdventureId { get; init; }

    public required string AdventureName { get; init; } = string.Empty;

    public required Guid LlmPresetId { get; init; }

    public required string LlmPresetName { get; init; } = string.Empty;

    public required string Title { get; init; } = string.Empty;

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public required List<ChatMessageResponseDto> Messages { get; init; } = [];
}

public class ChatSseChunk
{
    public required string Type { get; init; }

    public string? Content { get; init; }

    public ChatMessageResponseDto? Message { get; init; }
}

internal sealed class ChatService : IChatService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly KernelBuilderFactory _kernelBuilderFactory;
    private readonly IPluginFactory _pluginFactory;
    private readonly IServiceProvider _serviceProvider;

    public ChatService(
        ApplicationDbContext dbContext,
        KernelBuilderFactory kernelBuilderFactory,
        IPluginFactory pluginFactory,
        IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        _kernelBuilderFactory = kernelBuilderFactory;
        _pluginFactory = pluginFactory;
        _serviceProvider = serviceProvider;
    }

    public async Task<IEnumerable<ChatSessionResponseDto>> GetAllSessionsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ChatSessions
            .Include(s => s.Adventure)
            .Include(s => s.LlmPreset)
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Select(s => new ChatSessionResponseDto
            {
                Id = s.Id,
                AdventureId = s.AdventureId,
                AdventureName = s.Adventure.Name,
                LlmPresetId = s.LlmPresetId,
                LlmPresetName = s.LlmPreset.Name,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatSessionWithMessagesDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.ChatSessions
            .Include(s => s.Adventure)
            .Include(s => s.LlmPreset)
            .Include(s => s.Messages)
            .Where(s => s.Id == sessionId)
            .Select(s => new ChatSessionWithMessagesDto
            {
                Id = s.Id,
                AdventureId = s.AdventureId,
                AdventureName = s.Adventure.Name,
                LlmPresetId = s.LlmPresetId,
                LlmPresetName = s.LlmPreset.Name,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                Messages = s.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new ChatMessageResponseDto
                    {
                        Id = m.Id,
                        Role = m.Role,
                        Content = m.Content,
                        CreatedAt = m.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return session;
    }

    public async Task<ChatSessionResponseDto> CreateSessionAsync(ChatSessionDto dto, CancellationToken cancellationToken)
    {
        var adventure = await _dbContext.Adventures.FindAsync([dto.AdventureId], cancellationToken)
                        ?? throw new InvalidOperationException($"Adventure {dto.AdventureId} not found");

        var preset = await _dbContext.LlmPresets.FindAsync([dto.LlmPresetId], cancellationToken)
                     ?? throw new InvalidOperationException($"LLM Preset {dto.LlmPresetId} not found");

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            AdventureId = dto.AdventureId,
            LlmPresetId = dto.LlmPresetId,
            Title = dto.Title,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.ChatSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ChatSessionResponseDto
        {
            Id = session.Id,
            AdventureId = session.AdventureId,
            AdventureName = adventure.Name,
            LlmPresetId = session.LlmPresetId,
            LlmPresetName = preset.Name,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public async Task<ChatSessionResponseDto?> UpdatePresetAsync(Guid sessionId, Guid llmPresetId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.ChatSessions
            .Include(s => s.Adventure)
            .Include(s => s.LlmPreset)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null) return null;

        var preset = await _dbContext.LlmPresets.FindAsync([llmPresetId], cancellationToken)
                     ?? throw new InvalidOperationException($"LLM Preset {llmPresetId} not found");

        session.LlmPresetId = llmPresetId;
        session.LlmPreset = preset;
        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ChatSessionResponseDto
        {
            Id = session.Id,
            AdventureId = session.AdventureId,
            AdventureName = session.Adventure.Name,
            LlmPresetId = session.LlmPresetId,
            LlmPresetName = preset.Name,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.ChatSessions.FindAsync([sessionId], cancellationToken);
        if (session == null) return false;

        _dbContext.ChatSessions.Remove(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteLatestMessageAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var latestMessage = await _dbContext.ChatMessages
            .Where(m => m.ChatSessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestMessage == null) return false;

        _dbContext.ChatMessages.Remove(latestMessage);

        if (latestMessage.Role == "user")
        {
            var assistantReply = await _dbContext.ChatMessages
                .Where(m => m.ChatSessionId == sessionId && m.Role == "assistant" && m.CreatedAt > latestMessage.CreatedAt)
                .OrderBy(m => m.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (assistantReply != null)
            {
                _dbContext.ChatMessages.Remove(assistantReply);
            }
        }

        var session = await _dbContext.ChatSessions.FindAsync([sessionId], cancellationToken);
        if (session != null)
        {
            session.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async IAsyncEnumerable<ChatSseChunk> StreamMessageAsync(
        Guid sessionId,
        string userMessage,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.ChatSessions
            .Include(s => s.Adventure)
            .ThenInclude(a => a.MainCharacter)
            .Include(s => s.LlmPreset)
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            yield return new ChatSseChunk
            {
                Type = "error",
                Content = "Session not found"
            };

            yield break;
        }

        var adventure = session.Adventure;
        var preset = session.LlmPreset;

        var userMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId,
            Role = "user",
            Content = userMessage,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.ChatMessages.Add(userMsg);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var chatHistory = await BuildChatHistoryAsync(session, cancellationToken);

        var kernelBuilder = _kernelBuilderFactory.Create(preset);
        var kernel = kernelBuilder.Create();

        var adventureId = session.AdventureId;
        var latestScenes = await _dbContext.Scenes
            .Where(s => s.AdventureId == adventureId && s.CommitStatus == CommitStatus.Commited)
            .OrderByDescending(s => s.SequenceNumber)
            .Take(40)
            .ToArrayAsync(cancellationToken);

        var latestScene = latestScenes.OrderByDescending(x => x.SequenceNumber).First();
        ProcessExecutionContext.AdventureId.Value = adventureId;
        ProcessExecutionContext.SceneId.Value = latestScene.Id;

        var latestSummary = latestScenes.Where(x => !string.IsNullOrEmpty(x.Metadata.McStorySummary)).OrderByDescending(x => x.SequenceNumber).FirstOrDefault()?.Metadata.McStorySummary;
        if (!string.IsNullOrEmpty(latestSummary))
        {
            var message = $"""
                           <summary>
                           The summary of the adventure so fat:
                           {latestSummary}
                           </summary>
                           """;
            chatHistory.AddMessage(AuthorRole.User, message);
        }

        var formatted = string.Join("\n",
            latestScenes
                .OrderBy(x => x.SequenceNumber)
                .Take(WriterAgent.SceneContextCount)
                .Select(x => $"""
                              Time: {x.Metadata.Tracker!.Scene!.Time}
                              Location: {x.Metadata.Tracker.Scene.Location}
                              Weather: {x.Metadata.Tracker.Scene.Weather}
                              {x.NarrativeText}
                              """));

        var scenes = $"""
                     <latest_scenes>
                     The latest scenes in the adventures:
                     {formatted}
                     </latest_scenes>
                     """;
        chatHistory.AddMessage(AuthorRole.User, scenes);

        var generationContext = BuildGenerationContext(adventure, latestScene);
        var callerContext = new CallerContext(nameof(ChatService), adventureId, latestScene.Id);

        await _pluginFactory.AddPluginAsync<WorldKnowledgePlugin>(kernel, generationContext, callerContext);
        await _pluginFactory.AddPluginAsync<MainCharacterNarrativePlugin>(kernel, generationContext, callerContext);

        var chatCharacterPlugin = _serviceProvider.GetRequiredService<ChatCharacterPlugin>();
        await chatCharacterPlugin.SetupAsync(generationContext, callerContext);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(chatCharacterPlugin));

        var kernelBuilt = kernel.Build();
        var chatCompletionService = kernelBuilt.GetRequiredService<IChatCompletionService>();

        var responseBuilder = new StringBuilder();
        var settings = kernelBuilder.GetDefaultPromptExecutionSettings();

        await foreach (var chunk in chatCompletionService.GetStreamingChatMessageContentsAsync(
                           chatHistory,
                           settings,
                           kernelBuilt,
                           cancellationToken))
        {
            if (chunk.Content != null)
            {
                responseBuilder.Append(chunk.Content);
                yield return new ChatSseChunk
                {
                    Type = "chunk",
                    Content = chunk.Content
                };
            }
        }

        var fullResponse = responseBuilder.ToString();

        var assistantMsg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatSessionId = sessionId,
            Role = "assistant",
            Content = fullResponse,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.ChatMessages.Add(assistantMsg);

        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        yield return new ChatSseChunk
        {
            Type = "done",
            Message = new ChatMessageResponseDto
            {
                Id = assistantMsg.Id,
                Role = assistantMsg.Role,
                Content = assistantMsg.Content,
                CreatedAt = assistantMsg.CreatedAt
            }
        };
    }

    private GenerationContext BuildGenerationContext(Adventure adventure, Scene? latestScene)
    {
        var sceneContexts = new List<SceneContext>();
        if (latestScene != null)
        {
            sceneContexts.Add(SceneContext.CreateFromScene(latestScene));
        }

        var context = new GenerationContext
        {
            AdventureId = adventure.Id,
            PlayerAction = string.Empty,
            MainCharacter = adventure.MainCharacter,
            PromptPath = adventure.PromptPath,
            AdventureStartTime = adventure.AdventureStartTime,
            AgentLlmPreset = adventure.AgentLlmPresets.ToArray(),
            TrackerStructure = adventure.TrackerStructure,
            Characters = [],
            SceneContext = sceneContexts.ToArray()
        };

        return context;
    }

    private async Task<ChatHistory> BuildChatHistoryAsync(ChatSession session, CancellationToken cancellationToken)
    {
        var chatHistory = new ChatHistory();

        var systemPrompt = await BuildSystemPromptAsync(session, cancellationToken);
        chatHistory.AddSystemMessage(systemPrompt);

        foreach (var msg in session.Messages.OrderBy(m => m.CreatedAt))
        {
            switch (msg.Role)
            {
                case "user":
                    chatHistory.AddUserMessage(msg.Content);
                    break;
                case "assistant":
                    chatHistory.AddAssistantMessage(msg.Content);
                    break;
                case "system":
                    chatHistory.AddSystemMessage(msg.Content);
                    break;
            }
        }

        return chatHistory;
    }

    private async Task<string> BuildSystemPromptAsync(ChatSession session, CancellationToken cancellationToken)
    {
        var adventure = session.Adventure;
        var promptPath = adventure.PromptPath;

        var chatAgentPath = Path.Combine(promptPath, "ChatAgent.md");
        if (!File.Exists(chatAgentPath))
        {
            var defaultPath = Path.Combine("Prompts", "Default", "ChatAgent.md");
            if (File.Exists(defaultPath))
            {
                chatAgentPath = defaultPath;
            }
            else
            {
                chatAgentPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "Default", "ChatAgent.md");
            }
        }

        var promptTemplate = await File.ReadAllTextAsync(chatAgentPath, cancellationToken);

        var storyBiblePath = Path.Combine(promptPath, "StoryBible.md");
        var storyBible = File.Exists(storyBiblePath) ? await File.ReadAllTextAsync(storyBiblePath, cancellationToken) : string.Empty;

        var worldSettingsPath = Path.Combine(promptPath, "WorldSettings.md");
        var worldSettings = File.Exists(worldSettingsPath) ? await File.ReadAllTextAsync(worldSettingsPath, cancellationToken) : string.Empty;

        var jailbreakPath = Path.Combine(promptPath, "Jailbrake.md");
        var jailbreak = File.Exists(jailbreakPath) ? await File.ReadAllTextAsync(jailbreakPath, cancellationToken) : string.Empty;

        var prompt = promptTemplate
            .Replace("{{story_bible}}", storyBible)
            .Replace("{{world_setting}}", worldSettings)
            .Replace("{{jailbreak}}", jailbreak)
            .Replace("{{CHARACTER_NAME}}", adventure.MainCharacter.Name);

        var adventureId = session.AdventureId;
        var latestScene = await _dbContext.Scenes
            .Where(s => s.AdventureId == adventureId && s.CommitStatus == CommitStatus.Commited)
            .OrderByDescending(s => s.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestScene?.Metadata?.Tracker?.Scene != null)
        {
            var tracker = latestScene.Metadata.Tracker.Scene;
            prompt +=
                $"\n\n<current_scene_state>\nTime: {tracker.Time}\nLocation: {tracker.Location}\nWeather: {tracker.Weather}\nCharacters Present: {string.Join(", ", tracker.CharactersPresent ?? [])}\n</current_scene_state>";
        }

        prompt +=
            $"\n\n<adventure_info>\nAdventure: {adventure.Name}\nMain Character: {adventure.MainCharacter.Name}\n{adventure.MainCharacter.Description}\n</adventure_info>";

        return prompt;
    }
}