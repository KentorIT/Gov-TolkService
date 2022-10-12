using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddBrokerFeeByServiceTypePriceListRow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrokerFeeByServiceTypePriceListRows",
                columns: table => new
                {
                    BrokerFeeByServiceTypePriceListRowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CompetenceLevel = table.Column<int>(type: "int", nullable: false),
                    InterpreterLocation = table.Column<int>(type: "int", nullable: false),
                    FirstValidDate = table.Column<DateTime>(type: "date", nullable: false),
                    LastValidDate = table.Column<DateTime>(type: "date", nullable: false),
                    RegionGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerFeeByServiceTypePriceListRows", x => x.BrokerFeeByServiceTypePriceListRowId);
                    table.ForeignKey(
                        name: "FK_BrokerFeeByServiceTypePriceListRows_RegionGroups_RegionGroupId",
                        column: x => x.RegionGroupId,
                        principalTable: "RegionGroups",
                        principalColumn: "RegionGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrokerFeeByServiceTypePriceListRows_RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows",
                column: "RegionGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrokerFeeByServiceTypePriceListRows");
        }
    }
}
