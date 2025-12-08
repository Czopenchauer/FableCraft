using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;
using FableCraft.Infrastructure.Persistence.Entities.Adventure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;

using Testcontainers.PostgreSql;

using ILogger = Serilog.ILogger;

namespace FableCraft.Tests.Agents;

public class AgentIntegrationTests
{
    private static PostgreSqlContainer _postgresContainer = null!;
    private static IConfiguration _configuration = null!;
    private static ILogger _logger = null!;

    private IAgentKernel _agentKernel = null!;
    private ApplicationDbContext _dbContext = null!;
    private IDbContextFactory<ApplicationDbContext> _dbContextFactory = null!;
    private KernelBuilderFactory _kernelBuilderFactory = null!;
    private IRagSearch _ragSearch = null!;
    private Guid _testAdventureId;
    private LlmPreset _testPreset = null!;

    [Before(Class)]
    public async static Task InitializeClassAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("fablecraft_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgresContainer.StartAsync();
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        await using var dbContext = new ApplicationDbContext(dbOptions);
        await dbContext.Database.MigrateAsync();
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets<AgentIntegrationTests>()
            .AddEnvironmentVariables()
            .Build();

        Logger serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("./logs/AgentIntegrationTests.log")
            .CreateLogger();

        _logger = serilogLogger;
    }

    [After(Class)]
    public async static Task DisposeClassAsync()
    {
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Before(Test)]
    public async Task SetupTestAsync()
    {
        var loggerFactory = new SerilogLoggerFactory(_logger);

        _testPreset = new LlmPreset
        {
            Id = Guid.NewGuid(),
            Name = "Test Preset",
            Provider = _configuration["FableCraft:Server:LLM:Provider"] ?? "openai",
            ApiKey = _configuration["FableCraft:Server:LLM:ApiKey"] ?? throw new InvalidOperationException("LLM ApiKey not configured"),
            BaseUrl = _configuration["FableCraft:Server:LLM:BaseUrl"] ?? throw new InvalidOperationException("LLM BaseUrl not configured"),
            Model = _configuration["FableCraft:Server:LLM:Model"] ?? throw new InvalidOperationException("LLM Model not configured"),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _kernelBuilderFactory = new KernelBuilderFactory(loggerFactory);
        _agentKernel = new AgentKernel(_logger, new MockMessageDispatcher());
        _ragSearch = new MockRagSearch();

        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _dbContextFactory = new MockDbContextFactory(dbOptions);
        _dbContext = new ApplicationDbContext(dbOptions);
        await _dbContext.Database.MigrateAsync();

        // Seed test adventure with TrackerStructure
        _testAdventureId = Guid.NewGuid();
        Adventure testAdventure = AgentTestData.CreateTestAdventure(_testAdventureId);
        _dbContext.Adventures.Add(testAdventure);
        await _dbContext.SaveChangesAsync();
    }

    [After(Test)]
    public async Task TeardownTestAsync()
    {
        await _dbContext.DisposeAsync();
    }

    #region NarrativeDirectorAgent Tests

    [Test]
    public async Task NarrativeDirectorAgent_OutputMapsToNarrativeDirectorOutput()
    {
        // Arrange
        var agent = new NarrativeDirectorAgent(_agentKernel, _kernelBuilderFactory, _ragSearch, _logger);
        GenerationContext context = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext, _testPreset);

        // Act
        await agent.Invoke(context, CancellationToken.None);

        // Assert
        await Assert.That(context.NewNarrativeDirection).IsNotNull();
        await Assert.That(context.NewNarrativeDirection!.SceneMetadata).IsNotNull();
        await Assert.That(context.NewNarrativeDirection.SceneDirection).IsNotNull();
        await Assert.That(context.NewNarrativeDirection.Objectives).IsNotNull();
        await Assert.That(context.NewNarrativeDirection.Conflicts).IsNotNull();

        await Assert.That(context.NewNarrativeDirection.SceneMetadata.SceneNumber).IsGreaterThanOrEqualTo(0);
        await Assert.That(string.IsNullOrEmpty(context.NewNarrativeDirection.SceneMetadata.NarrativeAct)).IsFalse();
        await Assert.That(string.IsNullOrEmpty(context.NewNarrativeDirection.SceneMetadata.BeatType)).IsFalse();

        _logger.Information("NarrativeDirectorAgent output successfully mapped to NarrativeDirectorOutput");
        _logger.Information("Scene Number: {SceneNumber}, Beat Type: {BeatType}",
            context.NewNarrativeDirection.SceneMetadata.SceneNumber,
            context.NewNarrativeDirection.SceneMetadata.BeatType);
    }

    #endregion

    #region WriterAgent Tests

    [Test]
    public async Task WriterAgent_OutputMapsToGeneratedScene()
    {
        // Arrange
        var agent = new WriterAgent(_agentKernel, _logger, _kernelBuilderFactory, _ragSearch);
        GenerationContext context = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext, _testPreset);
        context.NewNarrativeDirection = AgentTestData.CreateSampleNarrativeDirectorOutput();

        // Act
        await agent.Invoke(context, CancellationToken.None);

        // Assert
        await Assert.That(context.NewScene).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(context.NewScene!.Scene)).IsFalse();
        await Assert.That(context.NewScene.Choices).IsNotNull();
        await Assert.That(context.NewScene.Choices).IsNotEmpty();

        _logger.Information("WriterAgent output successfully mapped to GeneratedScene");
        _logger.Information("Scene length: {Length} chars, Choices count: {Count}",
            context.NewScene.Scene.Length,
            context.NewScene.Choices.Length);
    }

    #endregion

    #region LoreCrafter Tests

    [Test]
    public async Task LoreCrafter_OutputMapsToGeneratedLore()
    {
        // Arrange
        var agent = new LoreCrafter(_agentKernel, _kernelBuilderFactory, _ragSearch);
        GenerationContext context = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext, _testPreset);
        LoreRequest loreRequest = AgentTestData.CreateSampleLoreRequest();

        // Act
        GeneratedLore result = await agent.Invoke(context, loreRequest, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(result.Title)).IsFalse();
        await Assert.That(string.IsNullOrEmpty(result.Text)).IsFalse();
        await Assert.That(string.IsNullOrEmpty(result.Summary)).IsFalse();

        _logger.Information("LoreCrafter output successfully mapped to GeneratedLore");
        _logger.Information("Lore Title: {Title}", result.Title);
    }

    #endregion

    #region LocationCrafter Tests

    [Test]
    public async Task LocationCrafter_OutputMapsToLocationGenerationResult()
    {
        // Arrange
        var agent = new LocationCrafter(_agentKernel, _kernelBuilderFactory, _ragSearch);
        GenerationContext context = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext, _testPreset);
        LocationRequest locationRequest = AgentTestData.CreateSampleLocationRequest();

        // Act
        LocationGenerationResult result = await agent.Invoke(context, locationRequest, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.EntityData).IsNotNull();
        await Assert.That(result.NarrativeData).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(result.EntityData.Name)).IsFalse();
        await Assert.That(string.IsNullOrEmpty(result.NarrativeData.ShortDescription)).IsFalse();

        _logger.Information("LocationCrafter output successfully mapped to LocationGenerationResult");
        _logger.Information("Location Name: {Name}, Type: {Type}",
            result.EntityData.Name,
            result.EntityData.Type);
    }

    #endregion

    #region CharacterCrafter Tests

    [Test]
    public async Task CharacterCrafter_OutputMapsToCharacterContext()
    {
        // Arrange
        var agent = new CharacterCrafter(_agentKernel, _dbContextFactory, _kernelBuilderFactory, _ragSearch);
        GenerationContext context = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext, _testPreset);
        CharacterRequest characterRequest = AgentTestData.CreateSampleCharacterRequest();

        // Act
        CharacterContext result = await agent.Invoke(context, characterRequest, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(result.Name)).IsFalse();
        await Assert.That(string.IsNullOrEmpty(result.Description)).IsFalse();
        await Assert.That(result.CharacterState).IsNotNull();
        await Assert.That(result.CharacterTracker).IsNotNull();

        _logger.Information("CharacterCrafter output successfully mapped to CharacterContext");
        _logger.Information("Character Name: {Name}", result.Name);
    }

    #endregion

    #region CharacterStateAgent Tests

    [Test]
    public async Task CharacterStateAgent_OutputMapsToCharacterStats()
    {
        // Arrange
        var agent = new CharacterStateAgent(_agentKernel, _dbContextFactory, _kernelBuilderFactory, _ragSearch);
        GenerationContext narrativeContext = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext, _testPreset);
        CharacterContext characterContext = AgentTestData.CreateSampleCharacterContext();

        // Act
        CharacterStats result = await agent.Invoke(narrativeContext, characterContext, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.CharacterIdentity).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(result.CharacterIdentity.FullName)).IsFalse();

        _logger.Information("CharacterStateAgent output successfully mapped to CharacterStats");
        _logger.Information("Updated Character State: {Name}", result.CharacterIdentity.FullName);
    }

    #endregion

    #region CharacterTrackerAgent Tests

    [Test]
    public async Task CharacterTrackerAgent_OutputMapsToCharacterTracker()
    {
        // Arrange
        var agent = new CharacterTrackerAgent(_agentKernel, _dbContextFactory, _kernelBuilderFactory, _ragSearch);
        GenerationContext narrativeContext = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext, _testPreset);
        CharacterContext characterContext = AgentTestData.CreateSampleCharacterContext();

        // Act
        CharacterTracker result = await agent.Invoke(narrativeContext, characterContext, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(result.Name)).IsFalse();

        _logger.Information("CharacterTrackerAgent output successfully mapped to CharacterTracker");
        _logger.Information("Updated Character Tracker: {Name}", result.Name);
    }

    #endregion

    #region TrackerAgent Tests

    [Test]
    public async Task TrackerAgent_OutputMapsToTracker()
    {
        // Arrange
        var agent = new TrackerAgent(_agentKernel, _dbContextFactory, _kernelBuilderFactory);
        GenerationContext context = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext, _testPreset);

        // Act
        Infrastructure.Persistence.Entities.Adventure.Tracker result = await agent.Invoke(context, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Story).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(result.Story.Location)).IsFalse();

        _logger.Information("TrackerAgent output successfully mapped to Tracker");
        _logger.Information("Story Location: {Location}, Weather: {Weather}",
            result.Story.Location,
            result.Story.Weather);
    }

    #endregion
}