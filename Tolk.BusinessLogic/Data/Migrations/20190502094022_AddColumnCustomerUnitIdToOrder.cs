using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddColumnCustomerUnitIdToOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerUnitId",
                table: "Orders",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerUnitId",
                table: "Orders",
                column: "CustomerUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CustomerUnits_CustomerUnitId",
                table: "Orders",
                column: "CustomerUnitId",
                principalTable: "CustomerUnits",
                principalColumn: "CustomerUnitId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CustomerUnits_CustomerUnitId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerUnitId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerUnitId",
                table: "Orders");
        }
    }
}
