using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class DeletedTravelCostsColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TravelCosts",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "ExpectedTravelCosts",
                table: "Requests");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TravelCosts",
                table: "Requisitions",
                type: "decimal(10, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedTravelCosts",
                table: "Requests",
                type: "decimal(10, 2)",
                nullable: true);
        }
    }
}
