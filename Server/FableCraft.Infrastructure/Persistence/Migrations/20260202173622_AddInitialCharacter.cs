using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInitialCharacter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Scenes_SceneId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_SceneId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SceneId",
                table: "Characters");

            migrationBuilder.AlterColumn<Guid>(
                name: "SceneId",
                table: "CharacterStates",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "IntroductionScene",
                table: "Characters",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "SceneId",
                table: "CharacterRelationships",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "InitialMainCharacterTracker",
                table: "Adventures",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_IntroductionScene",
                table: "Characters",
                column: "IntroductionScene");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Scenes_IntroductionScene",
                table: "Characters",
                column: "IntroductionScene",
                principalTable: "Scenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Scenes_IntroductionScene",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_IntroductionScene",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "InitialMainCharacterTracker",
                table: "Adventures");

            migrationBuilder.AlterColumn<Guid>(
                name: "SceneId",
                table: "CharacterStates",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "IntroductionScene",
                table: "Characters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SceneId",
                table: "Characters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "SceneId",
                table: "CharacterRelationships",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_SceneId",
                table: "Characters",
                column: "SceneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Characters_Scenes_SceneId",
                table: "Characters",
                column: "SceneId",
                principalTable: "Scenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
