using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RequisitionPriceRow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequisitionPriceRow",
                columns: table => new
                {
                    RequisitionId = table.Column<int>(nullable: false),
                    PriceListRowId = table.Column<int>(nullable: false),
                    StartAt = table.Column<DateTimeOffset>(nullable: false),
                    EndAt = table.Column<DateTimeOffset>(nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(10, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionPriceRow", x => new { x.RequisitionId, x.PriceListRowId });
                    table.ForeignKey(
                        name: "FK_RequisitionPriceRow_PriceListRows_PriceListRowId",
                        column: x => x.PriceListRowId,
                        principalTable: "PriceListRows",
                        principalColumn: "PriceListRowId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequisitionPriceRow_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "RequisitionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionPriceRow_PriceListRowId",
                table: "RequisitionPriceRow",
                column: "PriceListRowId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequisitionPriceRow");
        }
    }
}
