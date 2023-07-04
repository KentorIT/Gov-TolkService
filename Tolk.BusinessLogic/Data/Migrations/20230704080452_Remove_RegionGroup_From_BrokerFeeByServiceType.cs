using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    /// <inheritdoc />
    public partial class Remove_RegionGroup_From_BrokerFeeByServiceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BrokerFeeByServiceTypePriceListRows_RegionGroups_RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows");

            migrationBuilder.DropIndex(
                name: "IX_BrokerFeeByServiceTypePriceListRows_RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows");

            migrationBuilder.DropColumn(
                name: "RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrokerFeeByServiceTypePriceListRows_RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows",
                column: "RegionGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_BrokerFeeByServiceTypePriceListRows_RegionGroups_RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows",
                column: "RegionGroupId",
                principalTable: "RegionGroups",
                principalColumn: "RegionGroupId");
        }
    }
}
