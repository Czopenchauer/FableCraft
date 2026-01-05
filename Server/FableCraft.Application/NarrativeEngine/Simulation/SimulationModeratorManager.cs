using System.Diagnostics.CodeAnalysis;

using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FableCraft.Application.NarrativeEngine.Simulation;

[Experimental("SKEXP0110")]
internal sealed class SimulationModeratorManager : GroupChatManager
{
    public override ValueTask<GroupChatManagerResult<string>> FilterResults(ChatHistory history, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public override ValueTask<GroupChatManagerResult<string>> SelectNextAgent(ChatHistory history, GroupChatTeam team, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(ChatHistory history, CancellationToken cancellationToken = new CancellationToken())
    {
        return new ValueTask<GroupChatManagerResult<bool>>(new GroupChatManagerResult<bool>(false));
    }
}