using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintForScenes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scenes_AdventureId_SequenceNumber",
                table: "Scenes");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_AdventureId_SequenceNumber",
                table: "Scenes",
                columns: new[] { "AdventureId", "SequenceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scenes_AdventureId_SequenceNumber",
                table: "Scenes");

            migrationBuilder.CreateIndex(
                name: "IX_Scenes_AdventureId_SequenceNumber",
                table: "Scenes",
                columns: new[] { "AdventureId", "SequenceNumber" });
        }
    }
}
