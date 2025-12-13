using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromptPathToAdventure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromptPath",
                table: "AdventureAgentLlmPresets");

            migrationBuilder.AddColumn<string>(
                name: "PromptPath",
                table: "Adventures",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromptPath",
                table: "Adventures");

            migrationBuilder.AddColumn<string>(
                name: "PromptPath",
                table: "AdventureAgentLlmPresets",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
