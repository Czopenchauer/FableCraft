using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LlmCallLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdventureId = table.Column<Guid>(type: "uuid", nullable: true),
                    CallerName = table.Column<string>(type: "text", nullable: true),
                    RequestContent = table.Column<string>(type: "text", nullable: false),
                    ResponseContent = table.Column<string>(type: "text", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InputToken = table.Column<int>(type: "integer", nullable: true),
                    OutputToken = table.Column<int>(type: "integer", nullable: true),
                    TotalToken = table.Column<int>(type: "integer", nullable: true),
                    Duration = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmCallLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LlmCallLogs_AdventureId",
                table: "LlmCallLogs",
                column: "AdventureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LlmCallLogs");
        }
    }
}
