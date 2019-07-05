using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddCompetenceColumnsToLanguages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAuthorized",
                table: "Languages",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasEducated",
                table: "Languages",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasHealthcare",
                table: "Languages",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasLegal",
                table: "Languages",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAuthorized",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "HasEducated",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "HasHealthcare",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "HasLegal",
                table: "Languages");
        }
    }
}
