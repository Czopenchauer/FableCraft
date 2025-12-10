using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LorebookEntries_Scenes_SceneId",
                table: "LorebookEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_LorebookEntries_Scenes_SceneId",
                table: "LorebookEntries",
                column: "SceneId",
                principalTable: "Scenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LorebookEntries_Scenes_SceneId",
                table: "LorebookEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_LorebookEntries_Scenes_SceneId",
                table: "LorebookEntries",
                column: "SceneId",
                principalTable: "Scenes",
                principalColumn: "Id");
        }
    }
}
