using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeadFlagForCharacters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CharacterStates_CharacterId_SequenceNumber",
                table: "CharacterStates");

            migrationBuilder.DropIndex(
                name: "IX_CharacterStates_SequenceNumber",
                table: "CharacterStates");

            migrationBuilder.AddColumn<bool>(
                name: "IsDead",
                table: "CharacterStates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterStates_CharacterId_SequenceNumber_IsDead",
                table: "CharacterStates",
                columns: new[] { "CharacterId", "SequenceNumber", "IsDead" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CharacterStates_CharacterId_SequenceNumber_IsDead",
                table: "CharacterStates");

            migrationBuilder.DropColumn(
                name: "IsDead",
                table: "CharacterStates");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterStates_CharacterId_SequenceNumber",
                table: "CharacterStates",
                columns: new[] { "CharacterId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterStates_SequenceNumber",
                table: "CharacterStates",
                column: "SequenceNumber");
        }
    }
}
