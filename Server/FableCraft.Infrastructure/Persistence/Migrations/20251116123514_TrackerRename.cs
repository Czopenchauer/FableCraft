using System;
using FableCraft.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackerRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Trackers");

            migrationBuilder.CreateTable(
                name: "TrackerDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Structure = table.Column<TrackerStructure>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackerDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrackerDefinitions_Name",
                table: "TrackerDefinitions",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrackerDefinitions");

            migrationBuilder.CreateTable(
                name: "Trackers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Structure = table.Column<TrackerStructure>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trackers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trackers_Name",
                table: "Trackers",
                column: "Name");
        }
    }
}
