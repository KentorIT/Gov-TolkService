using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class NewIndexIsExtraInterpreter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_IsExtraInterpreterForOrderId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IsExtraInterpreterForOrderId",
                table: "Orders",
                column: "IsExtraInterpreterForOrderId",
                unique: true,
                filter: "[IsExtraInterpreterForOrderId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_IsExtraInterpreterForOrderId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IsExtraInterpreterForOrderId",
                table: "Orders",
                column: "IsExtraInterpreterForOrderId");
        }
    }
}
