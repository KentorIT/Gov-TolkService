using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderPriceRow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderPriceRow",
                columns: table => new
                {
                    OrderId = table.Column<int>(nullable: false),
                    PriceListRowId = table.Column<int>(nullable: false),
                    StartAt = table.Column<DateTimeOffset>(nullable: false),
                    EndAt = table.Column<DateTimeOffset>(nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(10, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPriceRow", x => new { x.OrderId, x.PriceListRowId });
                    table.ForeignKey(
                        name: "FK_OrderPriceRow_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderPriceRow_PriceListRows_PriceListRowId",
                        column: x => x.PriceListRowId,
                        principalTable: "PriceListRows",
                        principalColumn: "PriceListRowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPriceRow_PriceListRowId",
                table: "OrderPriceRow",
                column: "PriceListRowId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderPriceRow");
        }
    }
}
