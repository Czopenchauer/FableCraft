using Microsoft.SemanticKernel;

using IKernelBuilder = FableCraft.Infrastructure.Clients.IKernelBuilder;

namespace FableCraft.Application.NarrativeEngine;

public class WorldCrafter
{
    private readonly IKernelBuilder _kernelBuilder;

    public WorldCrafter(IKernelBuilder kernelBuilder)
    {
        _kernelBuilder = kernelBuilder;
    }
    
    public async Task<string> CraftWorldAsync(string prompt, CancellationToken cancellationToken)
    {
        var kernel = _kernelBuilder.WithBase().Build();
        
    }
}