using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeparateStatusesForRagAndSceneGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProcessingStatus",
                table: "Adventures",
                newName: "SceneGenerationStatus");

            migrationBuilder.AddColumn<string>(
                name: "RagProcessingStatus",
                table: "Adventures",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RagProcessingStatus",
                table: "Adventures");

            migrationBuilder.RenameColumn(
                name: "SceneGenerationStatus",
                table: "Adventures",
                newName: "ProcessingStatus");
        }
    }
}
