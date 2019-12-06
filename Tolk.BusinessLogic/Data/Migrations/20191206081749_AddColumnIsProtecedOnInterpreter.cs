using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddColumnIsProtecedOnInterpreter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProtected",
                table: "Interpreters",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProtected",
                table: "Interpreters");
        }
    }
}
