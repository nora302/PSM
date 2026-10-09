using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AjoutBenachrichtigungUndKrankenhaus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Benachrichtigungen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StandortId = table.Column<int>(type: "integer", nullable: false),
                    BewohnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Typ = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Nachricht = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EmpfaengerRolle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Gelesen = table.Column<bool>(type: "boolean", nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benachrichtigungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Benachrichtigungen_Standorte_StandortId",
                        column: x => x.StandortId,
                        principalTable: "Standorte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Benachrichtigungen_EmpfaengerRolle_Gelesen",
                table: "Benachrichtigungen",
                columns: new[] { "EmpfaengerRolle", "Gelesen" });

            migrationBuilder.CreateIndex(
                name: "IX_Benachrichtigungen_StandortId",
                table: "Benachrichtigungen",
                column: "StandortId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Benachrichtigungen");
        }
    }
}
