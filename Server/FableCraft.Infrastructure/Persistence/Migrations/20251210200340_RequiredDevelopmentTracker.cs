using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RequiredDevelopmentTracker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MainCharacter_Adventures_AdventureId",
                table: "MainCharacter");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MainCharacter",
                table: "MainCharacter");

            migrationBuilder.RenameTable(
                name: "MainCharacter",
                newName: "MainCharacters");

            migrationBuilder.RenameIndex(
                name: "IX_MainCharacter_AdventureId",
                table: "MainCharacters",
                newName: "IX_MainCharacters_AdventureId");

            migrationBuilder.AlterColumn<string>(
                name: "DevelopmentTracker",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MainCharacters",
                table: "MainCharacters",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MainCharacters_Adventures_AdventureId",
                table: "MainCharacters",
                column: "AdventureId",
                principalTable: "Adventures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MainCharacters_Adventures_AdventureId",
                table: "MainCharacters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MainCharacters",
                table: "MainCharacters");

            migrationBuilder.RenameTable(
                name: "MainCharacters",
                newName: "MainCharacter");

            migrationBuilder.RenameIndex(
                name: "IX_MainCharacters_AdventureId",
                table: "MainCharacter",
                newName: "IX_MainCharacter_AdventureId");

            migrationBuilder.AlterColumn<string>(
                name: "DevelopmentTracker",
                table: "Characters",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MainCharacter",
                table: "MainCharacter",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MainCharacter_Adventures_AdventureId",
                table: "MainCharacter",
                column: "AdventureId",
                principalTable: "Adventures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
