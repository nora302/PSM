using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AjoutBestellnummerUndTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BearbeitetVonBenutzerId",
                table: "Lebensmittelbestellungen",
                newName: "GesendetVonBenutzerId");

            migrationBuilder.AddColumn<DateTime>(
                name: "BearbeitungGestartetAm",
                table: "Lebensmittelbestellungen",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BearbeitungGestartetVonBenutzerId",
                table: "Lebensmittelbestellungen",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bestellnummer",
                table: "Lebensmittelbestellungen",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ErledigtVonBenutzerId",
                table: "Lebensmittelbestellungen",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GesendetAm",
                table: "Lebensmittelbestellungen",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lebensmittelbestellungen_Bestellnummer",
                table: "Lebensmittelbestellungen",
                column: "Bestellnummer",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lebensmittelbestellungen_Bestellnummer",
                table: "Lebensmittelbestellungen");

            migrationBuilder.DropColumn(
                name: "BearbeitungGestartetAm",
                table: "Lebensmittelbestellungen");

            migrationBuilder.DropColumn(
                name: "BearbeitungGestartetVonBenutzerId",
                table: "Lebensmittelbestellungen");

            migrationBuilder.DropColumn(
                name: "Bestellnummer",
                table: "Lebensmittelbestellungen");

            migrationBuilder.DropColumn(
                name: "ErledigtVonBenutzerId",
                table: "Lebensmittelbestellungen");

            migrationBuilder.DropColumn(
                name: "GesendetAm",
                table: "Lebensmittelbestellungen");

            migrationBuilder.RenameColumn(
                name: "GesendetVonBenutzerId",
                table: "Lebensmittelbestellungen",
                newName: "BearbeitetVonBenutzerId");
        }
    }
}
