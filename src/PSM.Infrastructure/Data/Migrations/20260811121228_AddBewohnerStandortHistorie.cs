using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBewohnerStandortHistorie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BewohnerStandortHistorien",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BewohnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlterStandortId = table.Column<int>(type: "integer", nullable: false),
                    NeuerStandortId = table.Column<int>(type: "integer", nullable: false),
                    AlteStandortBewohnerNummer = table.Column<int>(type: "integer", nullable: false),
                    NeueStandortBewohnerNummer = table.Column<int>(type: "integer", nullable: false),
                    AlteZimmernummer = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NeueZimmernummer = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GeaendertAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeaendertVonBenutzerId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BewohnerStandortHistorien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BewohnerStandortHistorien_Bewohner_BewohnerId",
                        column: x => x.BewohnerId,
                        principalTable: "Bewohner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BewohnerStandortHistorien_BewohnerId_GeaendertAm",
                table: "BewohnerStandortHistorien",
                columns: new[] { "BewohnerId", "GeaendertAm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BewohnerStandortHistorien");
        }
    }
}
