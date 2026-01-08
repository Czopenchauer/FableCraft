using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adventures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FirstSceneGuidance = table.Column<string>(type: "text", nullable: false),
                    AdventureStartTime = table.Column<string>(type: "text", nullable: false),
                    RagProcessingStatus = table.Column<string>(type: "text", nullable: false),
                    SceneGenerationStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastPlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WorldSettings = table.Column<string>(type: "text", nullable: false),
                    PromptPath = table.Column<string>(type: "text", nullable: false),
                    TrackerStructure = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adventures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetCharacterName = table.Column<string>(type: "text", nullable: false),
                    SourceCharacterName = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<string>(type: "text", nullable: false),
                    Event = table.Column<string>(type: "text", nullable: false),
                    SourceRead = table.Column<string>(type: "text", nullable: false),
                    Consumed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Chunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ContentHash = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    ChunkLocation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chunks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GenerationProcesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Context = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationProcesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LlmCallLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: true),
                    CallerName = table.Column<string>(type: "text", nullable: true),
                    RequestContent = table.Column<string>(type: "text", nullable: false),
                    ResponseContent = table.Column<string>(type: "text", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InputToken = table.Column<int>(type: "integer", nullable: true),
                    OutputToken = table.Column<int>(type: "integer", nullable: true),
                    TotalToken = table.Column<int>(type: "integer", nullable: true),
                    Duration = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmCallLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LlmPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: true),
                    TopP = table.Column<double>(type: "double precision", nullable: true),
                    TopK = table.Column<int>(type: "integer", nullable: true),
                    FrequencyPenalty = table.Column<double>(type: "double precision", nullable: true),
                    PresencePenalty = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmPresets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackerDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Structure = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackerDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Worldbooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worldbooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Importance = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Adventures_AdventureId",
                        column: x => x.AdventureId,
                        principalTable: "Adventures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MainCharacters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MainCharacters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MainCharacters_Adventures_AdventureId",
                        column: x => x.AdventureId,
                        principalTable: "Adventures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scenes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    AdventureSummary = table.Column<string>(type: "text", nullable: true),
                    NarrativeText = table.Column<string>(type: "text", nullable: false),
                    CommitStatus = table.Column<string>(type: "text", nullable: false),
                    EnrichmentStatus = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scenes_Adventures_AdventureId",
                        column: x => x.AdventureId,
                        principalTable: "Adventures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdventureAgentLlmPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: false),
                    LlmPresetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdventureAgentLlmPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdventureAgentLlmPresets_Adventures_AdventureId",
                        column: x => x.AdventureId,
                        principalTable: "Adventures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdventureAgentLlmPresets_LlmPresets_LlmPresetId",
                        column: x => x.LlmPresetId,
                        principalTable: "LlmPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lorebooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldbookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lorebooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lorebooks_Worldbooks_WorldbookId",
                        column: x => x.WorldbookId,
                        principalTable: "Worldbooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionDescription = table.Column<string>(type: "text", nullable: false),
                    Selected = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterActions_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: false),
                    SceneTracker = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Salience = table.Column<double>(type: "double precision", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterMemories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterMemories_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterMemories_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetCharacterName = table.Column<string>(type: "text", nullable: false),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdateTime = table.Column<string>(type: "text", nullable: true),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    Dynamic = table.Column<string>(type: "text", nullable: true),
                    Data = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterRelationships_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterRelationships_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSceneRewrites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    SceneTracker = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSceneRewrites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSceneRewrites_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterSceneRewrites_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CharacterStats = table.Column<string>(type: "text", nullable: false),
                    Tracker = table.Column<string>(type: "text", nullable: false),
                    SimulationMetadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterStates_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterStates_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LorebookEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: false),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LorebookEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LorebookEntries_Adventures_AdventureId",
                        column: x => x.AdventureId,
                        principalTable: "Adventures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LorebookEntries_Scenes_SceneId",
                        column: x => x.SceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdventureAgentLlmPresets_AdventureId_AgentName",
                table: "AdventureAgentLlmPresets",
                columns: new[] { "AdventureId", "AgentName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdventureAgentLlmPresets_LlmPresetId",
                table: "AdventureAgentLlmPresets",
                column: "LlmPresetId");

            migrationBuilder.CreateIndex(
                name: "IX_Adventures_Name",
                table: "Adventures",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterActions_SceneId",
                table: "CharacterActions",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterMemories_CharacterId",
                table: "CharacterMemories",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterMemories_CharacterId_Salience",
                table: "CharacterMemories",
                columns: new[] { "CharacterId", "Salience" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterMemories_SceneId",
                table: "CharacterMemories",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelationships_CharacterId_TargetCharacterName_Sequ~",
                table: "CharacterRelationships",
                columns: new[] { "CharacterId", "TargetCharacterName", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelationships_SceneId",
                table: "CharacterRelationships",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_AdventureId",
                table: "Characters",
                column: "AdventureId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSceneRewrites_CharacterId_SequenceNumber",
                table: "CharacterSceneRewrites",
                columns: new[] { "CharacterId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSceneRewrites_SceneId",
                table: "CharacterSceneRewrites",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterStates_CharacterId_SequenceNumber",
                table: "CharacterStates",
                columns: new[] { "CharacterId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterStates_SceneId",
                table: "CharacterStates",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterStates_SequenceNumber",
                table: "CharacterStates",
                column: "SequenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Chunks_EntityId",
                table: "Chunks",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Chunks_EntityId_ContentHash",
                table: "Chunks",
                columns: new[] { "EntityId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LlmCallLogs_AdventureId",
                table: "LlmCallLogs",
                column: "AdventureId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmPresets_Name",
                table: "LlmPresets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LorebookEntries_AdventureId",
                table: "LorebookEntries",
                column: "AdventureId");

            migrationBuilder.CreateIndex(
                name: "IX_LorebookEntries_SceneId",
                table: "LorebookEntries",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_Lorebooks_WorldbookId_Title",
                table: "Lorebooks",
                columns: new[] { "WorldbookId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MainCharacters_AdventureId",
                table: "MainCharacters",
                column: "AdventureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_AdventureId_SequenceNumber",
                table: "Scenes",
                columns: new[] { "AdventureId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_Id_SequenceNumber_CommitStatus",
                table: "Scenes",
                columns: new[] { "Id", "SequenceNumber", "CommitStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackerDefinitions_Name",
                table: "TrackerDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Worldbooks_Name",
                table: "Worldbooks",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdventureAgentLlmPresets");

            migrationBuilder.DropTable(
                name: "CharacterActions");

            migrationBuilder.DropTable(
                name: "CharacterEvents");

            migrationBuilder.DropTable(
                name: "CharacterMemories");

            migrationBuilder.DropTable(
                name: "CharacterRelationships");

            migrationBuilder.DropTable(
                name: "CharacterSceneRewrites");

            migrationBuilder.DropTable(
                name: "CharacterStates");

            migrationBuilder.DropTable(
                name: "Chunks");

            migrationBuilder.DropTable(
                name: "GenerationProcesses");

            migrationBuilder.DropTable(
                name: "LlmCallLogs");

            migrationBuilder.DropTable(
                name: "LorebookEntries");

            migrationBuilder.DropTable(
                name: "Lorebooks");

            migrationBuilder.DropTable(
                name: "MainCharacters");

            migrationBuilder.DropTable(
                name: "TrackerDefinitions");

            migrationBuilder.DropTable(
                name: "LlmPresets");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Scenes");

            migrationBuilder.DropTable(
                name: "Worldbooks");

            migrationBuilder.DropTable(
                name: "Adventures");
        }
    }
}
