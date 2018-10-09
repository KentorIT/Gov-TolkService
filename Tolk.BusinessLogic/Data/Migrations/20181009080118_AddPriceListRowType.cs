using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddPriceListRowType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceRowType",
                table: "PriceListRows");

            migrationBuilder.AddColumn<int>(
                name: "PriceListRowType",
                table: "PriceListRows",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceListRowType",
                table: "PriceListRows");

            migrationBuilder.AddColumn<int>(
                name: "PriceRowType",
                table: "PriceListRows",
                nullable: false,
                defaultValue: 0);
        }
    }
}
