using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterSimulation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorNotes",
                table: "Adventures");

            migrationBuilder.RenameColumn(
                name: "SceneTracker",
                table: "CharacterRelationships",
                newName: "UpdateTime");

            migrationBuilder.AddColumn<string>(
                name: "SimulationMetadata",
                table: "CharacterStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Importance",
                table: "Characters",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Dynamic",
                table: "CharacterRelationships",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WorldSettings",
                table: "Adventures",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CharacterEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetCharacterName = table.Column<string>(type: "text", nullable: false),
                    SourceCharacterName = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<string>(type: "text", nullable: false),
                    Event = table.Column<string>(type: "text", nullable: false),
                    SourceRead = table.Column<string>(type: "text", nullable: false),
                    Consumed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterEvents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterEvents");

            migrationBuilder.DropColumn(
                name: "SimulationMetadata",
                table: "CharacterStates");

            migrationBuilder.DropColumn(
                name: "Importance",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Dynamic",
                table: "CharacterRelationships");

            migrationBuilder.RenameColumn(
                name: "UpdateTime",
                table: "CharacterRelationships",
                newName: "SceneTracker");

            migrationBuilder.AlterColumn<string>(
                name: "WorldSettings",
                table: "Adventures",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "AuthorNotes",
                table: "Adventures",
                type: "text",
                nullable: true);
        }
    }
}
