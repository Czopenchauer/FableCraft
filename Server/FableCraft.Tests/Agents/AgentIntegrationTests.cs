using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Infrastructure.Clients;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using Serilog;
using Serilog.Extensions.Logging;

using Testcontainers.PostgreSql;

using IKernelBuilder = FableCraft.Infrastructure.Llm.IKernelBuilder;
using ILogger = Serilog.ILogger;

namespace FableCraft.Tests.Agents;

public class AgentIntegrationTests
{
    private static PostgreSqlContainer _postgresContainer = null!;
    private static IConfiguration _configuration = null!;
    private static ILogger _logger = null!;

    private IAgentKernel _agentKernel = null!;
    private IKernelBuilder _kernelBuilder = null!;
    private IRagSearch _ragSearch = null!;
    private ApplicationDbContext _dbContext = null!;
    private Guid _testAdventureId;

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

        var serilogLogger = new LoggerConfiguration()
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

        var llmConfig = new LlmConfiguration
        {
            ApiKey = _configuration["FableCraft:Server:LLM:ApiKey"] ?? throw new InvalidOperationException("LLM ApiKey not configured"),
            BaseUrl = _configuration["FableCraft:Server:LLM:BaseUrl"] ?? throw new InvalidOperationException("LLM BaseUrl not configured"),
            Model = _configuration["FableCraft:Server:LLM:Model"] ?? throw new InvalidOperationException("LLM Model not configured")
        };

        var options = Options.Create(llmConfig);

        _kernelBuilder = new OpenAiKernelBuilder(options, loggerFactory);
        _agentKernel = new AgentKernel(_kernelBuilder, _logger, new MockMessageDispatcher());
        _ragSearch = new MockRagSearch();

        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _dbContext = new ApplicationDbContext(dbOptions);
        await _dbContext.Database.EnsureCreatedAsync();

        // Seed test adventure with TrackerStructure
        _testAdventureId = Guid.NewGuid();
        var testAdventure = AgentTestData.CreateTestAdventure(_testAdventureId);
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
        var agent = new NarrativeDirectorAgent(_agentKernel, _kernelBuilder, _ragSearch);
        var context = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext);

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
            context.NewNarrativeDirection.SceneMetadata.SceneNumber, context.NewNarrativeDirection.SceneMetadata.BeatType);
    }

    #endregion

    #region WriterAgent Tests

    [Test]
    public async Task WriterAgent_OutputMapsToGeneratedScene()
    {
        // Arrange
        var agent = new WriterAgent(_agentKernel, _logger, _kernelBuilder, _ragSearch);
        var context = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext);
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
            context.NewScene.Scene.Length, context.NewScene.Choices.Length);
    }

    #endregion

    #region LoreCrafter Tests

    [Test]
    public async Task LoreCrafter_OutputMapsToGeneratedLore()
    {
        // Arrange
        var agent = new LoreCrafter(_agentKernel, _kernelBuilder, _ragSearch);
        var context = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext);
        var loreRequest = AgentTestData.CreateSampleLoreRequest();

        // Act
        var result = await agent.Invoke(context, loreRequest, CancellationToken.None);

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
        var agent = new LocationCrafter(_agentKernel, _kernelBuilder, _ragSearch);
        var context = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext);
        var locationRequest = AgentTestData.CreateSampleLocationRequest();

        // Act
        var result = await agent.Invoke(context, locationRequest, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.EntityData).IsNotNull();
        await Assert.That(result.NarrativeData).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(result.EntityData.Name)).IsFalse();
        await Assert.That(string.IsNullOrEmpty(result.NarrativeData.ShortDescription)).IsFalse();
        
        _logger.Information("LocationCrafter output successfully mapped to LocationGenerationResult");
        _logger.Information("Location Name: {Name}, Type: {Type}", 
            result.EntityData.Name, result.EntityData.Type);
    }

    #endregion

    #region CharacterCrafter Tests

    [Test]
    public async Task CharacterCrafter_OutputMapsToCharacterContext()
    {
        // Arrange
        var agent = new CharacterCrafter(_agentKernel, _dbContext, _kernelBuilder, _ragSearch);
        var context = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext);
        var characterRequest = AgentTestData.CreateSampleCharacterRequest();

        // Act
        var result = await agent.Invoke(context, characterRequest, CancellationToken.None);

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

    #region CharacterStateTracker Tests

    [Test]
    public async Task CharacterStateTracker_OutputMapsToCharacterContext()
    {
        // Arrange
        var agent = new CharacterStateTracker(_agentKernel, _dbContext, _kernelBuilder, _ragSearch);
        var narrativeContext = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext);
        var characterContext = AgentTestData.CreateSampleCharacterContext();

        // Act
        var result = await agent.Invoke(narrativeContext, characterContext, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(result.Name)).IsFalse();
        await Assert.That(result.CharacterState).IsNotNull();
        await Assert.That(result.CharacterTracker).IsNotNull();

        _logger.Information("CharacterStateTracker output successfully mapped to CharacterContext");
        _logger.Information("Updated Character: {Name}", result.Name);
    }

    #endregion

    #region TrackerAgent Tests

    [Test]
    public async Task TrackerAgent_OutputMapsToTracker()
    {
        // Arrange
        var agent = new TrackerAgent(_agentKernel, _dbContext, _kernelBuilder);
        var context = AgentTestData.CreateSampleNarrativeContext(_testAdventureId, _dbContext);

        // Act
        var result = await agent.Invoke(context, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Story).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(result.Story.Location)).IsFalse();

        _logger.Information("TrackerAgent output successfully mapped to Tracker");
        _logger.Information("Story Location: {Location}, Weather: {Weather}", 
            result.Story.Location, result.Story.Weather);
    }

    #endregion
}
