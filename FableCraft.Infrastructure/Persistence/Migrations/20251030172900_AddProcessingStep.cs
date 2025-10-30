using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KnowledgeGraphNodeId",
                table: "Worlds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "Worlds",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KnowledgeGraphNodeId",
                table: "Scenes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "Scenes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KnowledgeGraphNodeId",
                table: "LorebookEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "LorebookEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KnowledgeGraphNodeId",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KnowledgeGraphNodeId",
                table: "CharacterActions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "CharacterActions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KnowledgeGraphNodeId",
                table: "Worlds");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "Worlds");

            migrationBuilder.DropColumn(
                name: "KnowledgeGraphNodeId",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "Scenes");

            migrationBuilder.DropColumn(
                name: "KnowledgeGraphNodeId",
                table: "LorebookEntries");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "LorebookEntries");

            migrationBuilder.DropColumn(
                name: "KnowledgeGraphNodeId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "KnowledgeGraphNodeId",
                table: "CharacterActions");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "CharacterActions");
        }
    }
}
