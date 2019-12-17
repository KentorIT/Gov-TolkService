using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddCreatorIsInterpreterUserToOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CreatorIsInterpreterUser",
                table: "Orders",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CreatorIsInterpreterUser",
                table: "OrderGroups",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorIsInterpreterUser",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CreatorIsInterpreterUser",
                table: "OrderGroups");
        }
    }
}