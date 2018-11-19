using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddColumnsLanguages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "Languages",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ISO_639_1_Code",
                table: "Languages",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TellusName",
                table: "Languages",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "ISO_639_1_Code",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "TellusName",
                table: "Languages");
        }
    }
}
