using FableCraft.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJsonSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Structure",
                table: "TrackerDefinitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(TrackerStructure),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "SceneMetadata",
                table: "Scenes",
                type: "text",
                nullable: false,
                oldClrType: typeof(SceneMetadata),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "Tracker",
                table: "CharacterStates",
                type: "text",
                nullable: false,
                oldClrType: typeof(CharacterTracker),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "CharacterStats",
                table: "CharacterStates",
                type: "text",
                nullable: false,
                oldClrType: typeof(CharacterStats),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "TrackerStructure",
                table: "Adventures",
                type: "text",
                nullable: false,
                oldClrType: typeof(TrackerStructure),
                oldType: "jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TrackerStructure>(
                name: "Structure",
                table: "TrackerDefinitions",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<SceneMetadata>(
                name: "SceneMetadata",
                table: "Scenes",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<CharacterTracker>(
                name: "Tracker",
                table: "CharacterStates",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<CharacterStats>(
                name: "CharacterStats",
                table: "CharacterStates",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<TrackerStructure>(
                name: "TrackerStructure",
                table: "Adventures",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
