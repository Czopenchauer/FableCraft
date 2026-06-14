using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    GraphRagSettingsId = table.Column<Guid>(type: "uuid", nullable: true),
                    LlmPresetId = table.Column<Guid>(type: "uuid", nullable: true),
                    IndexingStatus = table.Column<string>(type: "text", nullable: false),
                    LastIndexedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_GraphRagSettings_GraphRagSettingsId",
                        column: x => x.GraphRagSettingsId,
                        principalTable: "GraphRagSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_LlmPresets_LlmPresetId",
                        column: x => x.LlmPresetId,
                        principalTable: "LlmPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectChatSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    LlmPresetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChatHistoryJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectChatSessions_LlmPresets_LlmPresetId",
                        column: x => x.LlmPresetId,
                        principalTable: "LlmPresets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectChatSessions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContentHash = table.Column<string>(type: "text", nullable: true),
                    IndexedContentHash = table.Column<string>(type: "text", nullable: true),
                    KnowledgeGraphNodeId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFiles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChatSessions_LlmPresetId",
                table: "ProjectChatSessions",
                column: "LlmPresetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChatSessions_ProjectId",
                table: "ProjectChatSessions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFiles_ProjectId",
                table: "ProjectFiles",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_GraphRagSettingsId",
                table: "Projects",
                column: "GraphRagSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_LlmPresetId",
                table: "Projects",
                column: "LlmPresetId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Name",
                table: "Projects",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectChatSessions");

            migrationBuilder.DropTable(
                name: "ProjectFiles");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
