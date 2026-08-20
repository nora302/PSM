using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPflegeSchicht : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Schicht",
                table: "Pflegedokumentationen",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Schicht",
                table: "Pflegedokumentationen");
        }
    }
}
