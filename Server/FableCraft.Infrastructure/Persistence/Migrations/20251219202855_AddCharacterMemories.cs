using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterMemories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Scenes_SceneId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_SceneId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_SequenceNumber",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CharacterId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CharacterStats",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SceneId",
                table: "Characters");

            migrationBuilder.RenameColumn(
                name: "KnowledgeGraphNodeId",
                table: "Chunks",
                newName: "ChunkLocation");

            migrationBuilder.RenameColumn(
                name: "Tracker",
                table: "Characters",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "SequenceNumber",
                table: "Characters",
                newName: "Version");

            migrationBuilder.CreateTable(
                name: "CharacterMemories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoryTracker = table.Column<string>(type: "text", nullable: false),
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
                    StoryTracker = table.Column<string>(type: "text", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
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
                    StoryTracker = table.Column<string>(type: "text", nullable: false),
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
                    Tracker = table.Column<string>(type: "text", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterMemories");

            migrationBuilder.DropTable(
                name: "CharacterRelationships");

            migrationBuilder.DropTable(
                name: "CharacterSceneRewrites");

            migrationBuilder.DropTable(
                name: "CharacterStates");

            migrationBuilder.RenameColumn(
                name: "ChunkLocation",
                table: "Chunks",
                newName: "KnowledgeGraphNodeId");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "Characters",
                newName: "SequenceNumber");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Characters",
                newName: "Tracker");

            migrationBuilder.AddColumn<Guid>(
                name: "CharacterId",
                table: "Characters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CharacterStats",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SceneId",
                table: "Characters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Characters_SceneId",
                table: "Characters",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_SequenceNumber",
                table: "Characters",
                column: "SequenceNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Scenes_SceneId",
                table: "Characters",
                column: "SceneId",
                principalTable: "Scenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
