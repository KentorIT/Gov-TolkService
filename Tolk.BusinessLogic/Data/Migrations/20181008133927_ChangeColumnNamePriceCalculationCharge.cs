using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ChangeColumnNamePriceCalculationCharge : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Charge",
                table: "PriceCalculationCharges",
                newName: "ChargePercentage");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChargePercentage",
                table: "PriceCalculationCharges",
                newName: "Charge");
        }
    }
}
