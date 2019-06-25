using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ConnectOrdersIfExtraInterpreter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IsExtraInterpreterForOrderId",
                table: "Orders",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IsExtraInterpreterForOrderId",
                table: "Orders",
                column: "IsExtraInterpreterForOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Orders_IsExtraInterpreterForOrderId",
                table: "Orders",
                column: "IsExtraInterpreterForOrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Orders_IsExtraInterpreterForOrderId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_IsExtraInterpreterForOrderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsExtraInterpreterForOrderId",
                table: "Orders");
        }
    }
}
