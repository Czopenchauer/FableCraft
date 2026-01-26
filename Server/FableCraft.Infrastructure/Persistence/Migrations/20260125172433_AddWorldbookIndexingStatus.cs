using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FableCraft.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldbookIndexingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IndexingError",
                table: "Worldbooks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IndexingStatus",
                table: "Worldbooks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "WorldbookId",
                table: "Adventures",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IndexingError",
                table: "Worldbooks");

            migrationBuilder.DropColumn(
                name: "IndexingStatus",
                table: "Worldbooks");

            migrationBuilder.DropColumn(
                name: "WorldbookId",
                table: "Adventures");
        }
    }
}
