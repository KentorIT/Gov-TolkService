using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RequestPriceRow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestPriceRow",
                columns: table => new
                {
                    RequestId = table.Column<int>(nullable: false),
                    PriceListRowId = table.Column<int>(nullable: false),
                    StartAt = table.Column<DateTimeOffset>(nullable: false),
                    EndAt = table.Column<DateTimeOffset>(nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(10, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestPriceRow", x => new { x.RequestId, x.PriceListRowId });
                    table.ForeignKey(
                        name: "FK_RequestPriceRow_PriceListRows_PriceListRowId",
                        column: x => x.PriceListRowId,
                        principalTable: "PriceListRows",
                        principalColumn: "PriceListRowId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestPriceRow_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestPriceRow_PriceListRowId",
                table: "RequestPriceRow",
                column: "PriceListRowId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestPriceRow");
        }
    }
}
