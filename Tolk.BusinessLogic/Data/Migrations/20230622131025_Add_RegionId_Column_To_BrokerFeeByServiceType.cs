using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_RegionId_Column_To_BrokerFeeByServiceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BrokerFeeByServiceTypePriceListRows_RegionGroups_RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows");

            migrationBuilder.AlterColumn<int>(
                name: "RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "BrokerFeeByServiceTypePriceListRows",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_BrokerFeeByServiceTypePriceListRows_RegionId",
                table: "BrokerFeeByServiceTypePriceListRows",
                column: "RegionId");

            migrationBuilder.AddForeignKey(
                name: "FK_BrokerFeeByServiceTypePriceListRows_RegionGroups_RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows",
                column: "RegionGroupId",
                principalTable: "RegionGroups",
                principalColumn: "RegionGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_BrokerFeeByServiceTypePriceListRows_Regions_RegionId",
                table: "BrokerFeeByServiceTypePriceListRows",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "RegionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BrokerFeeByServiceTypePriceListRows_RegionGroups_RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows");

            migrationBuilder.DropForeignKey(
                name: "FK_BrokerFeeByServiceTypePriceListRows_Regions_RegionId",
                table: "BrokerFeeByServiceTypePriceListRows");

            migrationBuilder.DropIndex(
                name: "IX_BrokerFeeByServiceTypePriceListRows_RegionId",
                table: "BrokerFeeByServiceTypePriceListRows");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "BrokerFeeByServiceTypePriceListRows");

            migrationBuilder.AlterColumn<int>(
                name: "RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BrokerFeeByServiceTypePriceListRows_RegionGroups_RegionGroupId",
                table: "BrokerFeeByServiceTypePriceListRows",
                column: "RegionGroupId",
                principalTable: "RegionGroups",
                principalColumn: "RegionGroupId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
