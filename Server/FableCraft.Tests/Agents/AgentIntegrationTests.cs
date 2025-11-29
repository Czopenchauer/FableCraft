using FableCraft.Application.NarrativeEngine;
using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using Serilog;
using Serilog.Extensions.Logging;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;
using ILogger = Serilog.ILogger;

namespace FableCraft.Tests.Agents;

/// <summary>
/// Integration tests for NarrativeEngine agents.
/// These tests verify that agents produce output that correctly maps to typed representations.
/// Tests use real LLM interaction without mocking.
/// </summary>
[Collection("IntegrationTests")]
public class AgentIntegrationTests : IAsyncLifetime
{
    private IAgentKernel _agentKernel = null!;
    private IKernelBuilder _kernelBuilder = null!;
    private ILogger _logger = null!;
    private IConfiguration _configuration = null!;
    private Kernel _kernel = null!;

    public Task InitializeAsync()
    {
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets<AgentIntegrationTests>()
            .AddEnvironmentVariables()
            .Build();

        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        _logger = serilogLogger;

        var loggerFactory = new SerilogLoggerFactory(serilogLogger);

        var llmConfig = new LlmConfiguration
        {
            ApiKey = _configuration["FableCraft:Server:LLM:ApiKey"] ?? throw new InvalidOperationException("LLM ApiKey not configured"),
            BaseUrl = _configuration["FableCraft:Server:LLM:BaseUrl"] ?? throw new InvalidOperationException("LLM BaseUrl not configured"),
            Model = _configuration["FableCraft:Server:LLM:Model"] ?? throw new InvalidOperationException("LLM Model not configured")
        };

        var options = Options.Create(llmConfig);

        _kernelBuilder = new OpenAiKernelBuilder(options, loggerFactory);
        _agentKernel = new AgentKernel(_kernelBuilder, _logger);
        _kernel = _kernelBuilder.WithBase().Build();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    #region NarrativeDirectorAgent Tests

    [Fact]
    public async Task NarrativeDirectorAgent_OutputMapsToNarrativeDirectorOutput()
    {
        // Arrange
        var agent = new NarrativeDirectorAgent(_agentKernel);
        var context = CreateSampleNarrativeContext();

        // Act
        var result = await agent.Invoke(context, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.SceneMetadata);
        Assert.NotNull(result.SceneDirection);
        Assert.NotNull(result.Objectives);
        Assert.NotNull(result.Conflicts);
        
        Assert.True(result.SceneMetadata.SceneNumber >= 0);
        Assert.False(string.IsNullOrEmpty(result.SceneMetadata.NarrativeAct));
        Assert.False(string.IsNullOrEmpty(result.SceneMetadata.BeatType));
        
        _logger.Information("NarrativeDirectorAgent output successfully mapped to NarrativeDirectorOutput");
        _logger.Information("Scene Number: {SceneNumber}, Beat Type: {BeatType}", 
            result.SceneMetadata.SceneNumber, result.SceneMetadata.BeatType);
    }

    #endregion

    #region WriterAgent Tests

    [Fact]
    public async Task WriterAgent_OutputMapsToGeneratedScene()
    {
        // Arrange
        var agent = new WriterAgent(_agentKernel, _logger);
        var context = CreateSampleNarrativeContext();
        var narrativeDirectorOutput = CreateSampleNarrativeDirectorOutput();
        var characterContexts = Array.Empty<CharacterContext>();

        // Act
        var result = await agent.Invoke(context, characterContexts, narrativeDirectorOutput, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Scene));
        Assert.NotNull(result.Choices);
        Assert.NotEmpty(result.Choices);
        
        _logger.Information("WriterAgent output successfully mapped to GeneratedScene");
        _logger.Information("Scene length: {Length} chars, Choices count: {Count}", 
            result.Scene.Length, result.Choices.Length);
    }

    #endregion

    #region LoreCrafter Tests

    [Fact]
    public async Task LoreCrafter_OutputMapsToGeneratedLore()
    {
        // Arrange
        var agent = new LoreCrafter(_agentKernel);
        var context = CreateSampleNarrativeContext();
        var loreRequest = CreateSampleLoreRequest();
        var characterContexts = Array.Empty<CharacterContext>();

        // Act
        var result = await agent.Invoke(_kernel, loreRequest, context, characterContexts, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Title));
        Assert.False(string.IsNullOrEmpty(result.Text));
        Assert.False(string.IsNullOrEmpty(result.Summary));
        
        _logger.Information("LoreCrafter output successfully mapped to GeneratedLore");
        _logger.Information("Lore Title: {Title}", result.Title);
    }

    #endregion

    #region LocationCrafter Tests

    [Fact]
    public async Task LocationCrafter_OutputMapsToLocationGenerationResult()
    {
        // Arrange
        var agent = new LocationCrafter(_agentKernel);
        var context = CreateSampleNarrativeContext();
        var locationRequest = CreateSampleLocationRequest();
        var characterContexts = Array.Empty<CharacterContext>();

        // Act
        var result = await agent.Invoke(_kernel, locationRequest, context, characterContexts, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.EntityData);
        Assert.NotNull(result.NarrativeData);
        Assert.False(string.IsNullOrEmpty(result.EntityData.Name));
        Assert.False(string.IsNullOrEmpty(result.NarrativeData.ShortDescription));
        
        _logger.Information("LocationCrafter output successfully mapped to LocationGenerationResult");
        _logger.Information("Location Name: {Name}, Type: {Type}", 
            result.EntityData.Name, result.EntityData.Type);
    }

    #endregion

    #region Helper Methods - Context Creation

    private NarrativeContext CreateSampleNarrativeContext()
    {
        return new NarrativeContext
        {
            AdventureId = Guid.NewGuid(),
            KernelKg = _kernel,
            StorySummary = "A young adventurer seeks to uncover the mystery of disappearing villagers in a remote mountain town.",
            PlayerAction = "I enter the tavern and look around for anyone who might have information.",
            CommonContext = """
                Adventure Summary: A young adventurer seeks to uncover the mystery of disappearing villagers in a remote mountain town.
                
                Current Scene: The protagonist has just arrived at the village of Thornhaven after a long journey.
                
                Setting: A medieval fantasy world with magic and mythical creatures.
                """,
            SceneContext = Array.Empty<SceneContext>(),
            Characters = new List<CharacterContext>(),
            NewLocations = Array.Empty<LocationGenerationResult>(),
            NewLore = Array.Empty<GeneratedLore>()
        };
    }

    private static NarrativeDirectorOutput CreateSampleNarrativeDirectorOutput()
    {
        return new NarrativeDirectorOutput
        {
            SceneMetadata = new SceneMetadata
            {
                SceneNumber = 1,
                NarrativeAct = "setup",
                BeatType = "discovery",
                TensionLevel = 3,
                Pacing = "building",
                EmotionalTarget = "curiosity"
            },
            SceneDirection = new SceneDirection
            {
                OpeningFocus = "The heavy oak door swings open, revealing a dimly lit tavern filled with suspicious glances.",
                RequiredElements = new List<string> { "smoky atmosphere", "nervous innkeeper", "hushed conversations" },
                PlotPointsToHit = new List<string> { "introduce the mystery", "hint at danger", "present an opportunity for information" },
                ToneGuidance = "mysterious and slightly threatening",
                PacingNotes = "slow build of tension",
                WorldbuildingOpportunity = "describe local customs",
                Foreshadowing = new List<string> { "hint at coming danger" }
            },
            Objectives = new Objectives(),
            Conflicts = new Conflicts()
        };
    }

    private static LoreRequest CreateSampleLoreRequest()
    {
        return new LoreRequest
        {
            Category = "location_history",
            Subject = "The Whispering Woods",
            Depth = "moderate",
            Tone = "mysterious and ominous",
            NarrativePurpose = "Explain why locals fear the forest and hint at supernatural phenomena",
            ConnectionPoints = new List<string> { "Thornhaven village", "disappearing villagers" },
            ConsistencyRequirements = new List<string> { "Must align with medieval fantasy setting" }
        };
    }

    private static LocationRequest CreateSampleLocationRequest()
    {
        return new LocationRequest
        {
            Type = "settlement",
            Scale = "building",
            Atmosphere = "rustic and mysterious",
            StrategicImportance = "Social hub for information gathering",
            Features = new List<string> { "common room", "private booths", "kitchen", "rooms for rent" },
            InhabitantTypes = new List<string> { "innkeeper", "travelers", "locals" },
            DangerLevel = 1,
            Accessibility = "open"
        };
    }

    #endregion
}
