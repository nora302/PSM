using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PSM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Standorte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Strasse = table.Column<string>(type: "text", nullable: false),
                    Hausnummer = table.Column<string>(type: "text", nullable: false),
                    Postleitzahl = table.Column<string>(type: "text", nullable: false),
                    Ort = table.Column<string>(type: "text", nullable: false),
                    Telefonnummer = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    IstAktiv = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Standorte", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bewohner",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StandortId = table.Column<int>(type: "integer", nullable: false),
                    StandortBewohnerNummer = table.Column<int>(type: "integer", nullable: false),
                    Vorname = table.Column<string>(type: "text", nullable: false),
                    Nachname = table.Column<string>(type: "text", nullable: false),
                    Geburtsdatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Zimmernummer = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bewohner", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bewohner_Standorte_StandortId",
                        column: x => x.StandortId,
                        principalTable: "Standorte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lebensmittelbestellungen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StandortId = table.Column<int>(type: "integer", nullable: false),
                    Bestelldatum = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Lieferdatum = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErstelltVonBenutzerId = table.Column<string>(type: "text", nullable: false),
                    BearbeitetVonBenutzerId = table.Column<string>(type: "text", nullable: true),
                    ErledigtAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Bemerkung = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lebensmittelbestellungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lebensmittelbestellungen_Standorte_StandortId",
                        column: x => x.StandortId,
                        principalTable: "Standorte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Allergien",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BewohnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Bemerkung = table.Column<string>(type: "text", nullable: false),
                    IstAktiv = table.Column<bool>(type: "boolean", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allergien", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Allergien_Bewohner_BewohnerId",
                        column: x => x.BewohnerId,
                        principalTable: "Bewohner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ernaehrungen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BewohnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Fruehstueck = table.Column<string>(type: "text", nullable: false),
                    Mittagessen = table.Column<string>(type: "text", nullable: false),
                    Abendessen = table.Column<string>(type: "text", nullable: false),
                    Besonderheiten = table.Column<string>(type: "text", nullable: false),
                    Kostform = table.Column<string>(type: "text", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ernaehrungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ernaehrungen_Bewohner_BewohnerId",
                        column: x => x.BewohnerId,
                        principalTable: "Bewohner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Essensausgaben",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BewohnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Mahlzeit = table.Column<string>(type: "text", nullable: false),
                    IstErledigt = table.Column<bool>(type: "boolean", nullable: false),
                    ErledigtAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErledigtVonBenutzerId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Essensausgaben", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Essensausgaben_Bewohner_BewohnerId",
                        column: x => x.BewohnerId,
                        principalTable: "Bewohner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pflegedokumentationen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BewohnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Inhalt = table.Column<string>(type: "text", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeaendertAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErstelltVonBenutzerId = table.Column<string>(type: "text", nullable: false),
                    GeaendertVonBenutzerId = table.Column<string>(type: "text", nullable: true),
                    MitSpracheErstellt = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pflegedokumentationen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pflegedokumentationen_Bewohner_BewohnerId",
                        column: x => x.BewohnerId,
                        principalTable: "Bewohner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bestellpositionen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LebensmittelbestellungId = table.Column<Guid>(type: "uuid", nullable: false),
                    Lebensmittelname = table.Column<string>(type: "text", nullable: false),
                    Menge = table.Column<decimal>(type: "numeric", nullable: false),
                    Einheit = table.Column<string>(type: "text", nullable: false),
                    Bemerkung = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bestellpositionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bestellpositionen_Lebensmittelbestellungen_Lebensmittelbest~",
                        column: x => x.LebensmittelbestellungId,
                        principalTable: "Lebensmittelbestellungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allergien_BewohnerId",
                table: "Allergien",
                column: "BewohnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Bestellpositionen_LebensmittelbestellungId",
                table: "Bestellpositionen",
                column: "LebensmittelbestellungId");

            migrationBuilder.CreateIndex(
                name: "IX_Bewohner_StandortId_StandortBewohnerNummer",
                table: "Bewohner",
                columns: new[] { "StandortId", "StandortBewohnerNummer" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ernaehrungen_BewohnerId",
                table: "Ernaehrungen",
                column: "BewohnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Essensausgaben_BewohnerId",
                table: "Essensausgaben",
                column: "BewohnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Lebensmittelbestellungen_StandortId",
                table: "Lebensmittelbestellungen",
                column: "StandortId");

            migrationBuilder.CreateIndex(
                name: "IX_Pflegedokumentationen_BewohnerId",
                table: "Pflegedokumentationen",
                column: "BewohnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Standorte_Code",
                table: "Standorte",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Allergien");

            migrationBuilder.DropTable(
                name: "Bestellpositionen");

            migrationBuilder.DropTable(
                name: "Ernaehrungen");

            migrationBuilder.DropTable(
                name: "Essensausgaben");

            migrationBuilder.DropTable(
                name: "Pflegedokumentationen");

            migrationBuilder.DropTable(
                name: "Lebensmittelbestellungen");

            migrationBuilder.DropTable(
                name: "Bewohner");

            migrationBuilder.DropTable(
                name: "Standorte");
        }
    }
}
