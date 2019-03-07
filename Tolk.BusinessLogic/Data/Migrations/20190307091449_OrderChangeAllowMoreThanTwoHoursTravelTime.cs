using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderChangeAllowMoreThanTwoHoursTravelTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowMoreThanTwoHoursTravelTime",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "AllowExceedingTravelCost",
                table: "Orders",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowExceedingTravelCost",
                table: "Orders");

            migrationBuilder.AddColumn<bool>(
                name: "AllowMoreThanTwoHoursTravelTime",
                table: "Orders",
                nullable: false,
                defaultValue: false);
        }
    }
}
