using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitChunkByDataset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chunks_EntityId_ContentHash",
                table: "Chunks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "CharacterStates");

            migrationBuilder.RenameColumn(
                name: "ChunkLocation",
                table: "Chunks",
                newName: "KnowledgeGraphNodeId");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Chunks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DatasetName",
                table: "Chunks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Chunks_EntityId_ContentHash_DatasetName",
                table: "Chunks",
                columns: new[] { "EntityId", "ContentHash", "DatasetName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chunks_EntityId_ContentHash_DatasetName",
                table: "Chunks");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Chunks");

            migrationBuilder.DropColumn(
                name: "DatasetName",
                table: "Chunks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Characters");

            migrationBuilder.RenameColumn(
                name: "KnowledgeGraphNodeId",
                table: "Chunks",
                newName: "ChunkLocation");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CharacterStates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Chunks_EntityId_ContentHash",
                table: "Chunks",
                columns: new[] { "EntityId", "ContentHash" },
                unique: true);
        }
    }
}
