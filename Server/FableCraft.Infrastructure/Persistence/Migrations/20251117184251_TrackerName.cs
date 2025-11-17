using FableCraft.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SceneStateJson",
                table: "Scenes");

            migrationBuilder.AddColumn<Tracker>(
                name: "Tracker",
                table: "Scenes",
                type: "jsonb",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tracker",
                table: "Scenes");

            migrationBuilder.AddColumn<string>(
                name: "SceneStateJson",
                table: "Scenes",
                type: "jsonb",
                nullable: true);
        }
    }
}
