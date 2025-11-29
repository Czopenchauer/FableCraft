using FableCraft.Application.NarrativeEngine;
using FableCraft.Application.NarrativeEngine.Agents;
using FableCraft.Application.NarrativeEngine.Models;
using FableCraft.Infrastructure.Llm;
using FableCraft.Infrastructure.Persistence;
using FableCraft.Infrastructure.Persistence.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

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
    private Kernel _kernel = null!;
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
        _agentKernel = new AgentKernel(_kernelBuilder, _logger);
        _kernel = _kernelBuilder.WithBase().Build();

        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _dbContext = new ApplicationDbContext(dbOptions);
        await _dbContext.Database.EnsureCreatedAsync();

        // Seed test adventure with TrackerStructure
        _testAdventureId = Guid.NewGuid();
        var testAdventure = CreateTestAdventure(_testAdventureId);
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
        var agent = new NarrativeDirectorAgent(_agentKernel);
        var context = CreateSampleNarrativeContext();

        // Act
        var result = await agent.Invoke(context, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.SceneMetadata).IsNotNull();
        await Assert.That(result.SceneDirection).IsNotNull();
        await Assert.That(result.Objectives).IsNotNull();
        await Assert.That(result.Conflicts).IsNotNull();
        
        await Assert.That(result.SceneMetadata.SceneNumber).IsGreaterThanOrEqualTo(0);
        await Assert.That(string.IsNullOrEmpty(result.SceneMetadata.NarrativeAct)).IsFalse();
        await Assert.That(string.IsNullOrEmpty(result.SceneMetadata.BeatType)).IsFalse();
        
        _logger.Information("NarrativeDirectorAgent output successfully mapped to NarrativeDirectorOutput");
        _logger.Information("Scene Number: {SceneNumber}, Beat Type: {BeatType}", 
            result.SceneMetadata.SceneNumber, result.SceneMetadata.BeatType);
    }

    #endregion

    #region WriterAgent Tests

    [Test]
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
        await Assert.That(result).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(result.Scene)).IsFalse();
        await Assert.That(result.Choices).IsNotNull();
        await Assert.That(result.Choices).IsNotEmpty();
        
        _logger.Information("WriterAgent output successfully mapped to GeneratedScene");
        _logger.Information("Scene length: {Length} chars, Choices count: {Count}", 
            result.Scene.Length, result.Choices.Length);
    }

    #endregion

    #region LoreCrafter Tests

    [Test]
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
        var agent = new LocationCrafter(_agentKernel);
        var context = CreateSampleNarrativeContext();
        var locationRequest = CreateSampleLocationRequest();
        var characterContexts = Array.Empty<CharacterContext>();

        // Act
        var result = await agent.Invoke(_kernel, locationRequest, context, characterContexts, CancellationToken.None);

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

    #region Helper Methods - Context Creation

    private NarrativeContext CreateSampleNarrativeContext(Guid? adventureId = null)
    {
        return new NarrativeContext
        {
            AdventureId = adventureId ?? _testAdventureId,
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

    private static Adventure CreateTestAdventure(Guid adventureId)
    {
        return new Adventure
        {
            Id = adventureId,
            Name = $"Test Adventure {adventureId}",
            FirstSceneGuidance = "The adventure begins in a mysterious tavern.",
            AdventureStartTime = "Evening, late autumn",
            ProcessingStatus = ProcessingStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            AuthorNotes = "Test adventure for integration tests",
            MainCharacter = new MainCharacter
            {
                Id = Guid.NewGuid(),
                AdventureId = adventureId,
                Name = "Aldric the Brave",
                Description = "A young adventurer with a mysterious past, seeking to prove themselves."
            },
            TrackerStructure = CreateTestTrackerStructure(),
            Lorebook = new List<LorebookEntry>()
        };
    }

    private static TrackerStructure CreateTestTrackerStructure()
    {
        return new TrackerStructure
        {
            Story = new[]
            {
                new FieldDefinition
                {
                    Name = "Location",
                    Type = FieldType.String,
                    Prompt = "Current location in the story",
                    DefaultValue = "Unknown"
                },
                new FieldDefinition
                {
                    Name = "Weather",
                    Type = FieldType.String,
                    Prompt = "Current weather conditions",
                    DefaultValue = "Clear"
                },
                new FieldDefinition
                {
                    Name = "TimeOfDay",
                    Type = FieldType.String,
                    Prompt = "Current time of day",
                    DefaultValue = "Day"
                }
            },
            CharactersPresent = new FieldDefinition
            {
                Name = "CharactersPresent",
                Type = FieldType.Array,
                Prompt = "List of characters currently present in the scene",
                DefaultValue = new List<string>()
            },
            MainCharacter = new[]
            {
                new FieldDefinition
                {
                    Name = "Health",
                    Type = FieldType.String,
                    Prompt = "Current health status",
                    DefaultValue = "Healthy"
                },
                new FieldDefinition
                {
                    Name = "Mood",
                    Type = FieldType.String,
                    Prompt = "Current emotional state",
                    DefaultValue = "Neutral"
                }
            },
            Characters = new[]
            {
                new FieldDefinition
                {
                    Name = "Disposition",
                    Type = FieldType.String,
                    Prompt = "Character's disposition toward the player",
                    DefaultValue = "Neutral"
                },
                new FieldDefinition
                {
                    Name = "Status",
                    Type = FieldType.String,
                    Prompt = "Character's current status",
                    DefaultValue = "Active"
                }
            }
        };
    }

    private static CharacterRequest CreateSampleCharacterRequest()
    {
        return new CharacterRequest
        {
            Role = "innkeeper",
            Importance = "scene_critical",
            Priority = "required",
            SceneRole = "Information provider and potential ally",
            Specifications = new CharacterSpecifications
            {
                Archetype = "wise mentor",
                Alignment = "neutral good",
                PowerLevel = "weaker",
                KeyTraits = new List<string> { "observant", "cautious", "knowledgeable" },
                RelationshipToPlayer = "neutral",
                NarrativePurpose = "Provide exposition and hints about the mystery",
                BackstoryDepth = "moderate"
            },
            Constraints = new CharacterConstraints
            {
                MustEnable = new List<string> { "information gathering" },
                ShouldHave = new List<string> { "local knowledge", "secrets" },
                CannotBe = new List<string> { "hostile", "overly helpful" }
            },
            ConnectionToExisting = new List<string> { "villagers", "disappearances" }
        };
    }

    private static GeneratedScene CreateSampleGeneratedScene()
    {
        return new GeneratedScene
        {
            Scene = """
                The tavern door creaks open as you step inside, revealing a dimly lit common room. 
                Smoke curls lazily from a stone fireplace, casting dancing shadows across weathered wooden beams. 
                A handful of patrons sit hunched over their drinks, their conversations falling silent as they 
                turn to regard you with suspicious eyes.
                
                Behind the bar, a middle-aged woman with sharp eyes and graying hair polishes a mug, 
                watching your every move. The air is thick with the smell of wood smoke and stale ale.
                
                "Stranger," she says, her voice carrying across the room. "We don't get many travelers 
                this time of year. What brings you to Thornhaven?"
                """,
            Choices = new[]
            {
                "Ask about the disappearances directly",
                "Order a drink and observe the room",
                "Introduce yourself as a traveler seeking shelter"
            }
        };
    }

    private static CharacterContext CreateSampleCharacterContext()
    {
        return new CharacterContext
        {
            Name = "Martha the Innkeeper",
            Description = "A sharp-eyed woman in her fifties who has run the Thornhaven tavern for decades.",
            CharacterState = new CharacterStats
            {
                CharacterIdentity = new CharacterIdentity
                {
                    FullName = "Martha Thornwood",
                    Aliases = new List<string> { "The Innkeeper", "Old Martha" },
                    Archetype = "wise mentor"
                },
            },
            CharacterTracker = new CharacterTracker
            {
                Name = "Martha the Innkeeper",
                AdditionalProperties = new Dictionary<string, object>
                {
                    { "Disposition", "Cautious" },
                    { "Status", "Active" }
                }
            }
        };
    }

    #endregion

    #region CharacterCrafter Tests

    [Test]
    public async Task CharacterCrafter_OutputMapsToCharacterContext()
    {
        // Arrange
        var agent = new CharacterCrafter(_agentKernel, _dbContext);
        var context = CreateSampleNarrativeContext();
        var characterRequest = CreateSampleCharacterRequest();

        // Act
        var result = await agent.Invoke(_kernel, context, characterRequest, CancellationToken.None);

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
        var agent = new CharacterStateTracker(_agentKernel, _dbContext, _kernelBuilder);
        var narrativeContext = CreateSampleNarrativeContext();
        var characterContext = CreateSampleCharacterContext();
        var scene = CreateSampleGeneratedScene();

        // Act
        var result = await agent.Invoke(
            _testAdventureId,
            narrativeContext,
            characterContext,
            scene,
            CancellationToken.None);

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
        var context = CreateSampleNarrativeContext();
        var scene = CreateSampleGeneratedScene();

        // Act
        var result = await agent.Invoke(context, scene, CancellationToken.None);

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
