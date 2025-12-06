using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldbook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Worldbooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worldbooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lorebooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorldbookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lorebooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lorebooks_Worldbooks_WorldbookId",
                        column: x => x.WorldbookId,
                        principalTable: "Worldbooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lorebooks_WorldbookId_Title",
                table: "Lorebooks",
                columns: new[] { "WorldbookId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Worldbooks_Name",
                table: "Worldbooks",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lorebooks");

            migrationBuilder.DropTable(
                name: "Worldbooks");
        }
    }
}
