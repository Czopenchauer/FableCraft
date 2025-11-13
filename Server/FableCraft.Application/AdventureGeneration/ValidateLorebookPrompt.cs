using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Serilog;

namespace FableCraft.Application.AdventureGeneration;

internal sealed class ValidateLorebookPrompt : IHostedService
{
    private readonly IOptions<AdventureCreationConfig> _configuration;
    private readonly ILogger _logger;

    public ValidateLorebookPrompt(IOptions<AdventureCreationConfig> configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var missingFiles = new List<string>();
        foreach ((var key, LorebookConfig value) in _configuration.Value.Lorebooks)
        {
            if (!File.Exists(value.GetPromptFileName()))
            {
                missingFiles.Add($"Missing lorebook {key} prompt file: {value.GetPromptFileName()}");
            }
        }

        if (missingFiles.Any())
        {
            var errorMessage = string.Join(Environment.NewLine, missingFiles);
            _logger.Fatal("Lorebook prompt validation failed: {ErrorMessage}", errorMessage);
            throw new InvalidOperationException($"Lorebook prompt validation failed: {errorMessage}");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}