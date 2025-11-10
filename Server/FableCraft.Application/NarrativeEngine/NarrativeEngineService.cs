#pragma warning disable SKEXP0110 // Semantic Kernel Agents are experimental

using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Interfaces;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Application.NarrativeEngine.Orchestration;
using FableCraft.Application.NarrativeEngine.Plugins;
using FableCraft.Infrastructure.Clients;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine;

/// <summary>
/// Main narrative engine service that orchestrates multi-agent scene generation
/// </summary>
public class NarrativeEngineService
{
    private readonly Kernel _kernel;
    private readonly IRagSearch _ragSearch;

    public NarrativeEngineService(Kernel kernel, IRagSearch ragSearch)
    {
        _kernel = kernel;
        _ragSearch = ragSearch;
    }

    /// <summary>
    /// Generate a new scene using multi-agent collaboration
    /// </summary>
    public async Task<SceneOutput> GenerateSceneAsync(
        string adventureId,
        IAgentContext context,
        List<CharacterProfile> sceneCharacters,
        CancellationToken cancellationToken = default)
    {
        // Create knowledge graph plugin
        var knowledgeGraphPlugin = new KnowledgeGraphPlugin(_ragSearch, adventureId);

        // Create kernel with plugin
        var kernelWithPlugin = _kernel.Clone();
        kernelWithPlugin.Plugins.Add(KernelPluginFactory.CreateFromObject(knowledgeGraphPlugin));

        // Initialize agents
        var agents = new List<Agent>
        {
            StoryWeaverAgent.Create(kernelWithPlugin, context),
            LoreCrafterAgent.Create(kernelWithPlugin, context),
            CharacterCrafterAgent.Create(kernelWithPlugin, context),
            QACriticAgent.Create(kernelWithPlugin, context)
        };

        // Add character agents for each character in the scene
        foreach (var character in sceneCharacters)
        {
            var relevantScenes = FilterRelevantScenes(context.RecentScenes, character);
            agents.Add(CharacterAgent.Create(kernelWithPlugin, character, relevantScenes));
        }

        // Create group chat manager
        var groupChat = new NarrativeGroupChatManager(agents);

        // Start the conversation with initial prompt
        var initialPrompt = BuildInitialPrompt(context);
        groupChat.AddChatMessage(new ChatMessageContent(
            AuthorRole.User,
            initialPrompt));

        // Execute the group chat
        var conversationHistory = new List<ChatMessageContent>();

        await foreach (var message in groupChat.InvokeAsync(cancellationToken))
        {
            conversationHistory.Add(message);

            // Log message for debugging
            Console.WriteLine($"[{message.AuthorName}]: {message.Content}");
        }

        // Extract scene output from conversation history
        var sceneOutput = ExtractSceneOutput(conversationHistory, groupChat);

        return sceneOutput;
    }

    /// <summary>
    /// Build initial prompt to start scene generation
    /// </summary>
    private string BuildInitialPrompt(IAgentContext context)
    {
        return $@"Generate the next scene in the narrative.

Current Arc Position: {context.CurrentArcPosition}

Recent Scene Summary:
{string.Join("\n", context.RecentScenes.TakeLast(3).Select(s => $"- {s.Summary}"))}

Story Weaver: Begin with Phase 1: Scene Planning.";
    }

    /// <summary>
    /// Filter scenes relevant to a specific character (scenes they participated in or would know about)
    /// </summary>
    private List<SceneContext> FilterRelevantScenes(List<SceneContext> allScenes, CharacterProfile character)
    {
        return allScenes.Where(scene =>
            scene.KeyEvents.Any(e => character.KnowledgeBoundaries.Any(k =>
                e.Contains(k, StringComparison.OrdinalIgnoreCase))) ||
            scene.CharacterDevelopments.ContainsKey(character.Name))
            .ToList();
    }

    /// <summary>
    /// Extract final scene output from conversation history
    /// </summary>
    private SceneOutput ExtractSceneOutput(
        List<ChatMessageContent> conversationHistory,
        NarrativeGroupChatManager groupChat)
    {
        // Find the final prose from StoryWeaver
        var storyWeaverMessages = conversationHistory
            .Where(m => m.AuthorName == "StoryWeaver")
            .ToList();

        var finalProseMessage = storyWeaverMessages
            .LastOrDefault(m => m.Content?.Contains("PROSE:", StringComparison.OrdinalIgnoreCase) == true);

        if (finalProseMessage == null)
        {
            throw new InvalidOperationException("No final prose found in conversation history");
        }

        var prose = ExtractSection(finalProseMessage.Content!, "PROSE:");
        var playerChoices = ExtractPlayerChoices(finalProseMessage.Content!);
        var metadata = ExtractMetadata(finalProseMessage.Content!);

        // Extract new entities from LoreCrafter and CharacterCrafter
        var newEntities = ExtractNewEntities(conversationHistory);

        // Extract character updates from Character Agents
        var characterUpdates = ExtractCharacterUpdates(conversationHistory);

        return new SceneOutput
        {
            SceneId = Guid.NewGuid().ToString(),
            Prose = prose,
            PlayerChoices = playerChoices,
            NewEntities = newEntities,
            CharacterUpdates = characterUpdates,
            Metadata = metadata,
            NarrativeArcPosition = metadata.GetValueOrDefault("Arc Position", "Unknown").ToString()!,
            ObjectivesAdvanced = ParseList(metadata.GetValueOrDefault("Objectives Advanced", "").ToString()!),
            NewPlotThreads = ParseList(metadata.GetValueOrDefault("New Plot Threads", "").ToString()!)
        };
    }

    /// <summary>
    /// Extract a section from content between markers
    /// </summary>
    private string ExtractSection(string content, string marker)
    {
        var startIndex = content.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1) return string.Empty;

        startIndex += marker.Length;

        // Find the next section marker or end
        var endMarkers = new[] { "PLAYER CHOICES:", "METADATA:", "HANDOFF" };
        var endIndex = content.Length;

        foreach (var endMarker in endMarkers)
        {
            var markerIndex = content.IndexOf(endMarker, startIndex, StringComparison.OrdinalIgnoreCase);
            if (markerIndex != -1 && markerIndex < endIndex)
            {
                endIndex = markerIndex;
            }
        }

        return content.Substring(startIndex, endIndex - startIndex).Trim();
    }

    /// <summary>
    /// Extract player choices from content
    /// </summary>
    private List<PlayerChoice> ExtractPlayerChoices(string content)
    {
        var choices = new List<PlayerChoice>();
        var choicesSection = ExtractSection(content, "PLAYER CHOICES:");

        if (string.IsNullOrEmpty(choicesSection)) return choices;

        var lines = choicesSection.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("1.") || trimmedLine.StartsWith("2.") || trimmedLine.StartsWith("3."))
            {
                var choiceText = trimmedLine.Substring(2).Trim();
                choices.Add(new PlayerChoice
                {
                    ChoiceId = Guid.NewGuid().ToString(),
                    ChoiceText = choiceText,
                    PotentialConsequences = new List<string>() // Could be enhanced with consequence extraction
                });
            }
        }

        return choices;
    }

    /// <summary>
    /// Extract metadata from content
    /// </summary>
    private Dictionary<string, object> ExtractMetadata(string content)
    {
        var metadata = new Dictionary<string, object>();
        var metadataSection = ExtractSection(content, "METADATA:");

        if (string.IsNullOrEmpty(metadataSection)) return metadata;

        var lines = metadataSection.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("- "))
            {
                var parts = trimmedLine.Substring(2).Split(':', 2);
                if (parts.Length == 2)
                {
                    metadata[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        return metadata;
    }

    /// <summary>
    /// Extract new entities created by LoreCrafter and CharacterCrafter
    /// </summary>
    private List<WorldEntity> ExtractNewEntities(List<ChatMessageContent> conversationHistory)
    {
        var entities = new List<WorldEntity>();

        var loreCrafterMessages = conversationHistory
            .Where(m => m.AuthorName == "LoreCrafter")
            .Select(m => m.Content ?? string.Empty);

        var characterCrafterMessages = conversationHistory
            .Where(m => m.AuthorName == "CharacterCrafter")
            .Select(m => m.Content ?? string.Empty);

        // Parse entities from messages (simplified - could be enhanced with structured parsing)
        foreach (var message in loreCrafterMessages.Concat(characterCrafterMessages))
        {
            if (message.Contains("LOCATION:", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("LORE:", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("ITEM:", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("CHARACTER:", StringComparison.OrdinalIgnoreCase))
            {
                // Entity found - would parse details here
                // For now, create a placeholder
                entities.Add(new WorldEntity
                {
                    EntityId = Guid.NewGuid().ToString(),
                    EntityType = "Parsed",
                    Name = "Entity from conversation",
                    Description = message.Substring(0, Math.Min(200, message.Length)),
                    Attributes = new Dictionary<string, string>(),
                    RelatedEntities = new List<string>()
                });
            }
        }

        return entities;
    }

    /// <summary>
    /// Extract character updates from Character Agent messages
    /// </summary>
    private Dictionary<string, object> ExtractCharacterUpdates(List<ChatMessageContent> conversationHistory)
    {
        var updates = new Dictionary<string, object>();

        var characterMessages = conversationHistory
            .Where(m => m.AuthorName?.StartsWith("Character_") == true);

        foreach (var message in characterMessages)
        {
            var characterName = message.AuthorName!.Replace("Character_", "").Replace("_", " ");
            var content = message.Content ?? string.Empty;

            // Extract emotional state updates
            if (content.Contains("EMOTIONAL STATE:", StringComparison.OrdinalIgnoreCase))
            {
                var emotionalState = ExtractSection(content, "EMOTIONAL STATE:");
                updates[$"{characterName}_EmotionalState"] = emotionalState;
            }

            // Extract new memories
            if (content.Contains("NEW MEMORY:", StringComparison.OrdinalIgnoreCase))
            {
                var newMemory = ExtractSection(content, "NEW MEMORY:");
                if (!updates.ContainsKey($"{characterName}_Memories"))
                {
                    updates[$"{characterName}_Memories"] = new List<string>();
                }
                ((List<string>)updates[$"{characterName}_Memories"]).Add(newMemory);
            }
        }

        return updates;
    }

    /// <summary>
    /// Parse comma-separated list
    /// </summary>
    private List<string> ParseList(string listString)
    {
        if (string.IsNullOrWhiteSpace(listString)) return new List<string>();

        return listString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
    }
}
