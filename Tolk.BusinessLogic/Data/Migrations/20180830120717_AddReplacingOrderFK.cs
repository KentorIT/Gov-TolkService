using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddReplacingOrderFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReplacingOrderId",
                table: "Orders",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ReplacingOrderId",
                table: "Orders",
                column: "ReplacingOrderId",
                unique: true,
                filter: "[ReplacingOrderId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Orders_ReplacingOrderId",
                table: "Orders",
                column: "ReplacingOrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Orders_ReplacingOrderId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ReplacingOrderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReplacingOrderId",
                table: "Orders");
        }
    }
}
