using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLlmConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ComplexPresetId",
                table: "Adventures",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FastPresetId",
                table: "Adventures",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LlmPresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: true),
                    TopP = table.Column<double>(type: "double precision", nullable: true),
                    TopK = table.Column<int>(type: "integer", nullable: true),
                    FrequencyPenalty = table.Column<double>(type: "double precision", nullable: true),
                    PresencePenalty = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmPresets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adventures_ComplexPresetId",
                table: "Adventures",
                column: "ComplexPresetId");

            migrationBuilder.CreateIndex(
                name: "IX_Adventures_FastPresetId",
                table: "Adventures",
                column: "FastPresetId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmPresets_Name",
                table: "LlmPresets",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Adventures_LlmPresets_ComplexPresetId",
                table: "Adventures",
                column: "ComplexPresetId",
                principalTable: "LlmPresets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Adventures_LlmPresets_FastPresetId",
                table: "Adventures",
                column: "FastPresetId",
                principalTable: "LlmPresets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Adventures_LlmPresets_ComplexPresetId",
                table: "Adventures");

            migrationBuilder.DropForeignKey(
                name: "FK_Adventures_LlmPresets_FastPresetId",
                table: "Adventures");

            migrationBuilder.DropTable(
                name: "LlmPresets");

            migrationBuilder.DropIndex(
                name: "IX_Adventures_ComplexPresetId",
                table: "Adventures");

            migrationBuilder.DropIndex(
                name: "IX_Adventures_FastPresetId",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "ComplexPresetId",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "FastPresetId",
                table: "Adventures");
        }
    }
}
