using System.Text;
using System.Text.Json;

using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.ProjectManagement.Models;
using FableCraft.ProjectManagement.Plugins;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using Serilog;

namespace FableCraft.ProjectManagement.Services;

public interface IProjectChatService
{
    Task<IEnumerable<ProjectChatSessionResponseDto>> GetAllSessionsAsync(Guid projectId, CancellationToken cancellationToken);
    Task<ProjectChatSessionResponseDto?> GetSessionAsync(Guid projectId, Guid sessionId, CancellationToken cancellationToken);
    Task<ProjectChatSessionResponseDto> CreateSessionAsync(Guid projectId, ProjectChatSessionDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteSessionAsync(Guid projectId, Guid sessionId, CancellationToken cancellationToken);
    Task<ProjectChatMessageEntry?> SendMessageAsync(Guid projectId, Guid sessionId, string userMessage, CancellationToken cancellationToken);
}

internal sealed class ProjectChatService : IProjectChatService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly KernelBuilderFactory _kernelBuilderFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public ProjectChatService(
        ApplicationDbContext dbContext,
        KernelBuilderFactory kernelBuilderFactory,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _dbContext = dbContext;
        _kernelBuilderFactory = kernelBuilderFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<ProjectChatSessionResponseDto>> GetAllSessionsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        return await _dbContext.ProjectChatSessions
            .Include(s => s.LlmPreset)
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Select(s => new ProjectChatSessionResponseDto
            {
                Id = s.Id,
                ProjectId = s.ProjectId,
                LlmPresetId = s.LlmPresetId,
                LlmPresetName = s.LlmPreset.Name,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectChatSessionResponseDto?> GetSessionAsync(Guid projectId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.ProjectChatSessions
            .Include(s => s.LlmPreset)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.ProjectId == projectId, cancellationToken);

        if (session is null) return null;

        return new ProjectChatSessionResponseDto
        {
            Id = session.Id,
            ProjectId = session.ProjectId,
            LlmPresetId = session.LlmPresetId,
            LlmPresetName = session.LlmPreset.Name,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public async Task<ProjectChatSessionResponseDto> CreateSessionAsync(Guid projectId, ProjectChatSessionDto dto, CancellationToken cancellationToken)
    {
        var project = await _dbContext.Projects.FindAsync([projectId], cancellationToken)
                      ?? throw new InvalidOperationException($"Project {projectId} not found");

        var preset = await _dbContext.LlmPresets.FindAsync([dto.LlmPresetId], cancellationToken)
                     ?? throw new InvalidOperationException($"LLM Preset {dto.LlmPresetId} not found");

        var session = new ProjectChatSession
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            LlmPresetId = dto.LlmPresetId,
            Title = dto.Title,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.ProjectChatSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectChatSessionResponseDto
        {
            Id = session.Id,
            ProjectId = session.ProjectId,
            LlmPresetId = session.LlmPresetId,
            LlmPresetName = preset.Name,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public async Task<bool> DeleteSessionAsync(Guid projectId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.ProjectChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.ProjectId == projectId, cancellationToken);

        if (session is null) return false;

        _dbContext.ProjectChatSessions.Remove(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ProjectChatMessageEntry?> SendMessageAsync(Guid projectId, Guid sessionId, string userMessage, CancellationToken cancellationToken)
    {
        var session = await _dbContext.ProjectChatSessions
            .Include(s => s.LlmPreset)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.ProjectId == projectId, cancellationToken);

        if (session is null) return null;

        var preset = session.LlmPreset;
        var chatHistory = DeserializeChatHistory(session.ChatHistoryJson);

        if (string.IsNullOrEmpty(session.ChatHistoryJson))
        {
            var systemPrompt = await BuildSystemPromptAsync(projectId, cancellationToken);
            chatHistory.AddSystemMessage(systemPrompt);
        }

        chatHistory.AddUserMessage(userMessage);

        var kernelBuilder = _kernelBuilderFactory.Create(preset);
        var kernel = kernelBuilder.Create();

        var filePlugin = _serviceProvider.GetRequiredService<ProjectFilePlugin>();
        filePlugin.SetProjectId(projectId);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(filePlugin));

        var searchPlugin = _serviceProvider.GetRequiredService<ProjectSearchPlugin>();
        searchPlugin.SetProjectId(projectId);
        kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(searchPlugin));

        var kernelBuilt = kernel.Build();
        var chatCompletionService = kernelBuilt.GetRequiredService<IChatCompletionService>();

        var settings = kernelBuilder.GetDefaultFunctionPromptExecutionSettings();

        var responseBuilder = new StringBuilder();
        await foreach (var chunk in chatCompletionService.GetStreamingChatMessageContentsAsync(
                           chatHistory, settings, kernelBuilt, cancellationToken))
        {
            if (chunk.Content is not null)
            {
                responseBuilder.Append(chunk.Content);
            }
        }

        var finalResponse = responseBuilder.ToString();

        chatHistory.AddAssistantMessage(finalResponse);
        _logger.Information(chatHistory.ToJsonString());

        session.ChatHistoryJson = chatHistory.ToJsonString();
        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectChatMessageEntry
        {
            Role = "assistant",
            Content = finalResponse
        };
    }

    private static ChatHistory DeserializeChatHistory(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new ChatHistory();
        try
        {
            return JsonSerializer.Deserialize<ChatHistory>(json, JsonExtensions.JsonSerializerOptions) ?? new ChatHistory();
        }
        catch
        {
            return new ChatHistory();
        }
    }

    private async Task<string> BuildSystemPromptAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var assembly = typeof(ProjectChatService).Assembly;
        var resourceName = "FableCraft.ProjectManagement.Prompts.ProjectAssistant.md";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return "You are a worldbuilding assistant. Help the user create and refine content for their project.";
        }

        using var reader = new StreamReader(stream);
        var prompt = await reader.ReadToEndAsync(cancellationToken);

        var files = await _dbContext.ProjectFiles
            .Where(f => f.ProjectId == projectId)
            .Select(f => new { f.Name, f.Category })
            .ToListAsync(cancellationToken);

        if (files.Count > 0)
        {
            var fileList = string.Join("\n", files.Select(f => $"- {f.Name} ({f.Category ?? "uncategorized"})"));
            prompt += $"\n\n<existing_files>\nThe following files already exist in this project:\n{fileList}\n</existing_files>";
        }

        return prompt;
    }
}