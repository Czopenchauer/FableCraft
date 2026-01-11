using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GenerationProcessStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Step",
                table: "GenerationProcesses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "IntroductionScene",
                table: "Characters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SceneId",
                table: "Characters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Characters_Scenes_SceneId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_SceneId",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Step",
                table: "GenerationProcesses");

            migrationBuilder.DropColumn(
                name: "IntroductionScene",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SceneId",
                table: "Characters");
        }
    }
}
