using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPflegeDokumentationDatumUniqueSchicht : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pflegedokumentationen_BewohnerId",
                table: "Pflegedokumentationen");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Datum",
                table: "Pflegedokumentationen",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_Pflegedokumentationen_BewohnerId_Datum",
                table: "Pflegedokumentationen",
                columns: new[] { "BewohnerId", "Datum" });

            migrationBuilder.CreateIndex(
                name: "IX_Pflegedokumentationen_BewohnerId_Datum_Schicht",
                table: "Pflegedokumentationen",
                columns: new[] { "BewohnerId", "Datum", "Schicht" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pflegedokumentationen_BewohnerId_Datum",
                table: "Pflegedokumentationen");

            migrationBuilder.DropIndex(
                name: "IX_Pflegedokumentationen_BewohnerId_Datum_Schicht",
                table: "Pflegedokumentationen");

            migrationBuilder.DropColumn(
                name: "Datum",
                table: "Pflegedokumentationen");

            migrationBuilder.CreateIndex(
                name: "IX_Pflegedokumentationen_BewohnerId",
                table: "Pflegedokumentationen",
                column: "BewohnerId");
        }
    }
}
