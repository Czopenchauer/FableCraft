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
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Background = table.Column<string>(type: "text", nullable: false),
                    KnowledgeGraphNodeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProcessingStatus = table.Column<string>(type: "text", nullable: false),
                    StatsJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Adventures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WorldDescription = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastPlayedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessingStatus = table.Column<string>(type: "text", nullable: false),
                    KnowledgeGraphNodeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AuthorNotes = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adventures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Adventures_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LorebookEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KnowledgeGraphNodeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProcessingStatus = table.Column<string>(type: "text", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Scenes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldId = table.Column<Guid>(type: "uuid", nullable: true),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    NarrativeText = table.Column<string>(type: "text", nullable: false),
                    SceneStateJson = table.Column<string>(type: "jsonb", nullable: false),
                    KnowledgeGraphNodeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProcessingStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousSceneId = table.Column<Guid>(type: "uuid", nullable: true),
                    NextSceneId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scenes_Adventures_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Adventures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Scenes_Scenes_NextSceneId",
                        column: x => x.NextSceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Scenes_Scenes_PreviousSceneId",
                        column: x => x.PreviousSceneId,
                        principalTable: "Scenes",
                        principalColumn: "Id");
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

            migrationBuilder.CreateIndex(
                name: "IX_Adventures_CharacterId",
                table: "Adventures",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterActions_SceneId",
                table: "CharacterActions",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_LorebookEntries_AdventureId",
                table: "LorebookEntries",
                column: "AdventureId");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_NextSceneId",
                table: "Scenes",
                column: "NextSceneId");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_PreviousSceneId",
                table: "Scenes",
                column: "PreviousSceneId");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_WorldId",
                table: "Scenes",
                column: "WorldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterActions");

            migrationBuilder.DropTable(
                name: "LorebookEntries");

            migrationBuilder.DropTable(
                name: "Scenes");

            migrationBuilder.DropTable(
                name: "Adventures");

            migrationBuilder.DropTable(
                name: "Characters");
        }
    }
}
