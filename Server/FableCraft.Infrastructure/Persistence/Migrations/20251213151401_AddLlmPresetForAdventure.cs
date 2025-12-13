using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLlmPresetForAdventure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Adventures_LlmPresets_ComplexPresetId",
                table: "Adventures");

            migrationBuilder.DropForeignKey(
                name: "FK_Adventures_LlmPresets_FastPresetId",
                table: "Adventures");

            migrationBuilder.DropIndex(
                name: "IX_Adventures_ComplexPresetId",
                table: "Adventures");

            migrationBuilder.DropIndex(
                name: "IX_Adventures_FastPresetId",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "ComplexPresetId",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "FastPresetId",
                table: "Adventures");

            migrationBuilder.CreateTable(
                name: "AdventureAgentLlmPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: false),
                    LlmPresetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentName = table.Column<string>(type: "text", nullable: false),
                    PromptPath = table.Column<string>(type: "text", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_AdventureAgentLlmPresets_AdventureId_AgentName",
                table: "AdventureAgentLlmPresets",
                columns: new[] { "AdventureId", "AgentName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdventureAgentLlmPresets_LlmPresetId",
                table: "AdventureAgentLlmPresets",
                column: "LlmPresetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdventureAgentLlmPresets");

            migrationBuilder.AddColumn<Guid>(
                name: "ComplexPresetId",
                table: "Adventures",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FastPresetId",
                table: "Adventures",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Adventures_ComplexPresetId",
                table: "Adventures",
                column: "ComplexPresetId");

            migrationBuilder.CreateIndex(
                name: "IX_Adventures_FastPresetId",
                table: "Adventures",
                column: "FastPresetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Adventures_LlmPresets_ComplexPresetId",
                table: "Adventures",
                column: "ComplexPresetId",
                principalTable: "LlmPresets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Adventures_LlmPresets_FastPresetId",
                table: "Adventures",
                column: "FastPresetId",
                principalTable: "LlmPresets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
