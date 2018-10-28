using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RenameRequisitionColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UseRequestPriceRows",
                table: "Requisitions",
                newName: "RequestOrReplacingOrderPeriodUsed");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequestOrReplacingOrderPeriodUsed",
                table: "Requisitions",
                newName: "UseRequestPriceRows");
        }
    }
}
