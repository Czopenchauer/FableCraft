using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorldbookIndexingDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Worldbooks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastIndexedAt",
                table: "Worldbooks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Lorebooks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "LorebookSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldbookId = table.Column<Guid>(type: "uuid", nullable: false),
                    LorebookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<int>(type: "integer", nullable: false),
                    IndexedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LorebookSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LorebookSnapshots_Worldbooks_WorldbookId",
                        column: x => x.WorldbookId,
                        principalTable: "Worldbooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LorebookSnapshots_WorldbookId",
                table: "LorebookSnapshots",
                column: "WorldbookId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LorebookSnapshots");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Worldbooks");

            migrationBuilder.DropColumn(
                name: "LastIndexedAt",
                table: "Worldbooks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Lorebooks");
        }
    }
}
