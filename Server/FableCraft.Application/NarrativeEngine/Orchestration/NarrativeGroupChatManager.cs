#pragma warning disable SKEXP0110 // Semantic Kernel Agents are experimental

using FableCraft.Application.NarrativeEngine.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Orchestration;

/// <summary>
/// Custom Group Chat orchestrator for narrative scene generation
/// Orchestrates conversation flow between Story Weaver, entity crafters, character agents, and QA
/// </summary>
public class NarrativeGroupChatManager
{
    private const int MaxIterations = 20;
    private const int MaxRevisionLoops = 2;

    private int _revisionCount = 0;
    private string? _currentPhase = "Planning";
    private readonly Dictionary<string, int> _agentTurnCount = new();
    private readonly AgentGroupChat _groupChat;
    private readonly List<Agent> _agents;

    public NarrativeGroupChatManager(List<Agent> agents)
    {
        _agents = agents;
        _groupChat = new AgentGroupChat(agents.ToArray())
        {
            ExecutionSettings = new AgentGroupChatSettings
            {
                SelectionStrategy = new NarrativeSelectionStrategy(this),
                TerminationStrategy = new NarrativeTerminationStrategy(this)
            }
        };
    }

    /// <summary>
    /// Select the next agent to speak based on conversation state and explicit handoffs
    /// </summary>
    internal Agent SelectNextAgent(IReadOnlyList<ChatMessageContent> history)
    {
        // Check for explicit handoffs in the last message
        if (history.Count > 0)
        {
            var lastMessage = history[^1];
            var content = lastMessage.Content ?? string.Empty;

            // Handle explicit handoffs
            if (content.Contains("HANDOFF TO QA_CRITIC", StringComparison.OrdinalIgnoreCase))
            {
                _currentPhase = "QA Review";
                return FindAgent("QA_Critic");
            }

            if (content.Contains("HANDOFF TO StoryWeaver", StringComparison.OrdinalIgnoreCase))
            {
                _currentPhase = "Story Composition";
                return FindAgent("StoryWeaver");
            }

            // Handle entity requests
            if (content.Contains("REQUESTING LoreCrafter:", StringComparison.OrdinalIgnoreCase))
            {
                _currentPhase = "Entity Creation";
                return FindAgent("LoreCrafter");
            }

            if (content.Contains("REQUESTING CharacterCrafter:", StringComparison.OrdinalIgnoreCase))
            {
                _currentPhase = "Character Creation";
                return FindAgent("CharacterCrafter");
            }

            // Handle character roleplay direction
            if (content.Contains("CHARACTER[", StringComparison.OrdinalIgnoreCase))
            {
                _currentPhase = "Roleplay";
                var characterName = ExtractCharacterName(content);
                if (!string.IsNullOrEmpty(characterName))
                {
                    var characterAgent = FindAgent($"Character_{characterName}");
                    if (characterAgent != null)
                    {
                        return characterAgent;
                    }
                }
            }

            // Handle revision requests
            if (content.Contains("REVISION REQUESTED FROM StoryWeaver", StringComparison.OrdinalIgnoreCase))
            {
                _revisionCount++;
                _currentPhase = "Revision";
                return FindAgent("StoryWeaver");
            }
        }

        // Default phase-based selection
        return _currentPhase switch
        {
            "Planning" => FindAgent("StoryWeaver"),
            "Entity Creation" => FindAgent("LoreCrafter"),
            "Character Creation" => FindAgent("CharacterCrafter"),
            "Roleplay" => SelectRoleplayAgent(history),
            "Story Composition" => FindAgent("StoryWeaver"),
            "QA Review" => FindAgent("QA_Critic"),
            "Revision" => FindAgent("StoryWeaver"),
            _ => FindAgent("StoryWeaver")
        };
    }

    /// <summary>
    /// Determine if the conversation should terminate
    /// </summary>
    internal bool ShouldTerminate(IReadOnlyList<ChatMessageContent> history)
    {
        if (history.Count == 0)
        {
            return false;
        }

        var lastMessage = history[^1];
        var content = lastMessage.Content ?? string.Empty;

        // Check for QA approval
        if (content.Contains("APPROVED - SCENE_COMPLETE", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check for max revision loops exceeded
        if (_revisionCount >= MaxRevisionLoops)
        {
            // Escalate to human or force approval
            return true;
        }

        // Check for max iterations
        if (history.Count >= MaxIterations)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Add a message to the group chat
    /// </summary>
    public void AddChatMessage(ChatMessageContent message)
    {
        _groupChat.AddChatMessage(message);
    }

    /// <summary>
    /// Invoke the group chat and get responses
    /// </summary>
    public IAsyncEnumerable<ChatMessageContent> InvokeAsync(CancellationToken cancellationToken = default)
    {
        return _groupChat.InvokeAsync(cancellationToken);
    }

    /// <summary>
    /// Find an agent by name or prefix
    /// </summary>
    internal Agent FindAgent(string nameOrPrefix)
    {
        var agent = _agents.FirstOrDefault(a =>
            a.Name?.Equals(nameOrPrefix, StringComparison.OrdinalIgnoreCase) == true ||
            a.Name?.StartsWith(nameOrPrefix, StringComparison.OrdinalIgnoreCase) == true);

        return agent ?? _agents.First(a => a.Name == "StoryWeaver");
    }

    /// <summary>
    /// Select the next character agent during roleplay phase
    /// </summary>
    private Agent SelectRoleplayAgent(IReadOnlyList<ChatMessageContent> history)
    {
        // Find all character agents
        var characterAgents = _agents.Where(a => a.Name.StartsWith("Character_")).ToList();

        if (characterAgents.Count == 0)
        {
            // No character agents, return to StoryWeaver
            _currentPhase = "Story Composition";
            return FindAgent("StoryWeaver");
        }

        // Check if all characters have had a turn in this roleplay phase
        var roleplayMessages = history
            .Reverse()
            .TakeWhile(m => !(m.Content?.Contains("CHARACTER[") ?? false))
            .ToList();

        var charactersThatSpoke = roleplayMessages
            .Select(m => m.AuthorName)
            .Where(name => name?.StartsWith("Character_") == true)
            .Distinct()
            .ToList();

        // If all characters spoke, return to StoryWeaver for prose composition
        if (charactersThatSpoke.Count >= characterAgents.Count)
        {
            _currentPhase = "Story Composition";
            return FindAgent("StoryWeaver");
        }

        // Select a character that hasn't spoken yet
        var nextCharacter = characterAgents.FirstOrDefault(a =>
            !charactersThatSpoke.Contains(a.Name));

        return nextCharacter ?? characterAgents.First();
    }

    /// <summary>
    /// Extract character name from CHARACTER[Name] directive
    /// </summary>
    private string? ExtractCharacterName(string content)
    {
        var startIndex = content.IndexOf("CHARACTER[", StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1) return null;

        startIndex += "CHARACTER[".Length;
        var endIndex = content.IndexOf("]", startIndex);
        if (endIndex == -1) return null;

        return content.Substring(startIndex, endIndex - startIndex).Replace(" ", "_");
    }

    /// <summary>
    /// Track agent turns for debugging and monitoring
    /// </summary>
    private void TrackAgentTurn(string agentName)
    {
        if (!_agentTurnCount.ContainsKey(agentName))
        {
            _agentTurnCount[agentName] = 0;
        }
        _agentTurnCount[agentName]++;
    }

    /// <summary>
    /// Get current phase for monitoring
    /// </summary>
    public string GetCurrentPhase() => _currentPhase ?? "Unknown";

    /// <summary>
    /// Get revision count for monitoring
    /// </summary>
    public int GetRevisionCount() => _revisionCount;

    /// <summary>
    /// Get agent turn statistics
    /// </summary>
    public IReadOnlyDictionary<string, int> GetAgentTurnCounts() => _agentTurnCount;
}

/// <summary>
/// Selection strategy for narrative generation
/// </summary>
internal class NarrativeSelectionStrategy : SelectionStrategy
{
    private readonly NarrativeGroupChatManager _manager;

    public NarrativeSelectionStrategy(NarrativeGroupChatManager manager)
    {
        _manager = manager;
    }

    protected override Task<Agent> SelectAgentAsync(IReadOnlyList<Agent> agents, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
    {
        var selectedAgent = _manager.SelectNextAgent(history);
        return Task.FromResult(selectedAgent);
    }
}

/// <summary>
/// Termination strategy for narrative generation
/// </summary>
internal class NarrativeTerminationStrategy : TerminationStrategy
{
    private readonly NarrativeGroupChatManager _manager;

    public NarrativeTerminationStrategy(NarrativeGroupChatManager manager)
    {
        _manager = manager;
    }

    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
    {
        var shouldTerminate = _manager.ShouldTerminate(history);
        return Task.FromResult(shouldTerminate);
    }
}
