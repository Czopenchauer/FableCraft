using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrackerDefinitions_Name",
                table: "TrackerDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerDefinitions_Name",
                table: "TrackerDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chunks_EntityId_ContentHash",
                table: "Chunks",
                columns: new[] { "EntityId", "ContentHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Adventures_Name",
                table: "Adventures",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrackerDefinitions_Name",
                table: "TrackerDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_Chunks_EntityId_ContentHash",
                table: "Chunks");

            migrationBuilder.DropIndex(
                name: "IX_Adventures_Name",
                table: "Adventures");

            migrationBuilder.CreateIndex(
                name: "IX_TrackerDefinitions_Name",
                table: "TrackerDefinitions",
                column: "Name");
        }
    }
}
