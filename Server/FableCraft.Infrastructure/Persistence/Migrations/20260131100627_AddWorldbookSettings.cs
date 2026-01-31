using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldbookSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GraphRagSettingsId",
                table: "Worldbooks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GraphRagSettingsId",
                table: "Adventures",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GraphRagSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LlmProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LlmModel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LlmEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LlmApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LlmApiVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LlmMaxTokens = table.Column<int>(type: "integer", nullable: false),
                    LlmRateLimitEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LlmRateLimitRequests = table.Column<int>(type: "integer", nullable: false),
                    LlmRateLimitInterval = table.Column<int>(type: "integer", nullable: false),
                    EmbeddingProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmbeddingModel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmbeddingEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmbeddingApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmbeddingApiVersion = table.Column<string>(type: "text", nullable: true),
                    EmbeddingDimensions = table.Column<int>(type: "integer", nullable: false),
                    EmbeddingMaxTokens = table.Column<int>(type: "integer", nullable: false),
                    EmbeddingBatchSize = table.Column<int>(type: "integer", nullable: false),
                    HuggingfaceTokenizer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphRagSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Worldbooks_GraphRagSettingsId",
                table: "Worldbooks",
                column: "GraphRagSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_Adventures_GraphRagSettingsId",
                table: "Adventures",
                column: "GraphRagSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphRagSettings_Name",
                table: "GraphRagSettings",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Adventures_GraphRagSettings_GraphRagSettingsId",
                table: "Adventures",
                column: "GraphRagSettingsId",
                principalTable: "GraphRagSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Worldbooks_GraphRagSettings_GraphRagSettingsId",
                table: "Worldbooks",
                column: "GraphRagSettingsId",
                principalTable: "GraphRagSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Adventures_GraphRagSettings_GraphRagSettingsId",
                table: "Adventures");

            migrationBuilder.DropForeignKey(
                name: "FK_Worldbooks_GraphRagSettings_GraphRagSettingsId",
                table: "Worldbooks");

            migrationBuilder.DropTable(
                name: "GraphRagSettings");

            migrationBuilder.DropIndex(
                name: "IX_Worldbooks_GraphRagSettingsId",
                table: "Worldbooks");

            migrationBuilder.DropIndex(
                name: "IX_Adventures_GraphRagSettingsId",
                table: "Adventures");

            migrationBuilder.DropColumn(
                name: "GraphRagSettingsId",
                table: "Worldbooks");

            migrationBuilder.DropColumn(
                name: "GraphRagSettingsId",
                table: "Adventures");
        }
    }
}
