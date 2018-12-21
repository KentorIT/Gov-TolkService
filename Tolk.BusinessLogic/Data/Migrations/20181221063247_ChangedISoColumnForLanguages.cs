using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ChangedISoColumnForLanguages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ISO_639_1_Code",
                table: "Languages",
                maxLength: 3,
                nullable: true);
            migrationBuilder.RenameColumn(
                name: "ISO_639_1_Code",
                table: "Languages",
                newName: "ISO_639_Code");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ISO_639_Code",
                table: "Languages",
                maxLength: 2,
                nullable: true);
            migrationBuilder.RenameColumn(
                name: "ISO_639_Code",
                table: "Languages",
                newName: "ISO_639_1_Code");
        }
    }
}
