using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AjoutLebensmittelKatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LebensmittelArtikel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IstAktiv = table.Column<bool>(type: "boolean", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErstelltVonBenutzerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LebensmittelArtikel", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LebensmittelArtikel_Name",
                table: "LebensmittelArtikel",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LebensmittelArtikel");
        }
    }
}
