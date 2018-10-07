using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class PriceRowChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderPriceRows_PriceListRows_PriceListRowId",
                table: "OrderPriceRows");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestPriceRows_PriceListRows_PriceListRowId",
                table: "RequestPriceRows");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionPriceRows_PriceListRows_PriceListRowId",
                table: "RequisitionPriceRows");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "RequisitionPriceRows");

            migrationBuilder.DropColumn(
                name: "IsBrokerFee",
                table: "RequisitionPriceRows");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "RequestPriceRows");

            migrationBuilder.DropColumn(
                name: "IsBrokerFee",
                table: "RequestPriceRows");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "OrderPriceRows");

            migrationBuilder.DropColumn(
                name: "IsBrokerFee",
                table: "OrderPriceRows");

            migrationBuilder.RenameColumn(
                name: "TotalPrice",
                table: "RequisitionPriceRows",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "TotalPrice",
                table: "RequestPriceRows",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "TotalPrice",
                table: "OrderPriceRows",
                newName: "Price");

            migrationBuilder.AlterColumn<int>(
                name: "PriceListRowId",
                table: "RequisitionPriceRows",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<int>(
                name: "PriceCalculationChargeId",
                table: "RequisitionPriceRows",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceRowType",
                table: "RequisitionPriceRows",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "RequisitionPriceRows",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "PriceListRowId",
                table: "RequestPriceRows",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<int>(
                name: "PriceCalculationChargeId",
                table: "RequestPriceRows",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceRowType",
                table: "RequestPriceRows",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "RequestPriceRows",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "PriceListRowId",
                table: "OrderPriceRows",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<int>(
                name: "PriceCalculationChargeId",
                table: "OrderPriceRows",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceRowType",
                table: "OrderPriceRows",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "OrderPriceRows",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PriceCalculationCharges",
                columns: table => new
                {
                    PriceCalculationChargeId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    StartDate = table.Column<DateTime>(nullable: false),
                    EndDate = table.Column<DateTime>(nullable: false),
                    Charge = table.Column<decimal>(type: "decimal(10, 2)", nullable: false),
                    ChargeTypeId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceCalculationCharges", x => x.PriceCalculationChargeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionPriceRows_PriceCalculationChargeId",
                table: "RequisitionPriceRows",
                column: "PriceCalculationChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestPriceRows_PriceCalculationChargeId",
                table: "RequestPriceRows",
                column: "PriceCalculationChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPriceRows_PriceCalculationChargeId",
                table: "OrderPriceRows",
                column: "PriceCalculationChargeId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderPriceRows_PriceCalculationCharges_PriceCalculationChargeId",
                table: "OrderPriceRows",
                column: "PriceCalculationChargeId",
                principalTable: "PriceCalculationCharges",
                principalColumn: "PriceCalculationChargeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderPriceRows_PriceListRows_PriceListRowId",
                table: "OrderPriceRows",
                column: "PriceListRowId",
                principalTable: "PriceListRows",
                principalColumn: "PriceListRowId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestPriceRows_PriceCalculationCharges_PriceCalculationChargeId",
                table: "RequestPriceRows",
                column: "PriceCalculationChargeId",
                principalTable: "PriceCalculationCharges",
                principalColumn: "PriceCalculationChargeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestPriceRows_PriceListRows_PriceListRowId",
                table: "RequestPriceRows",
                column: "PriceListRowId",
                principalTable: "PriceListRows",
                principalColumn: "PriceListRowId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionPriceRows_PriceCalculationCharges_PriceCalculationChargeId",
                table: "RequisitionPriceRows",
                column: "PriceCalculationChargeId",
                principalTable: "PriceCalculationCharges",
                principalColumn: "PriceCalculationChargeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionPriceRows_PriceListRows_PriceListRowId",
                table: "RequisitionPriceRows",
                column: "PriceListRowId",
                principalTable: "PriceListRows",
                principalColumn: "PriceListRowId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderPriceRows_PriceCalculationCharges_PriceCalculationChargeId",
                table: "OrderPriceRows");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderPriceRows_PriceListRows_PriceListRowId",
                table: "OrderPriceRows");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestPriceRows_PriceCalculationCharges_PriceCalculationChargeId",
                table: "RequestPriceRows");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestPriceRows_PriceListRows_PriceListRowId",
                table: "RequestPriceRows");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionPriceRows_PriceCalculationCharges_PriceCalculationChargeId",
                table: "RequisitionPriceRows");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionPriceRows_PriceListRows_PriceListRowId",
                table: "RequisitionPriceRows");

            migrationBuilder.DropTable(
                name: "PriceCalculationCharges");

            migrationBuilder.DropIndex(
                name: "IX_RequisitionPriceRows_PriceCalculationChargeId",
                table: "RequisitionPriceRows");

            migrationBuilder.DropIndex(
                name: "IX_RequestPriceRows_PriceCalculationChargeId",
                table: "RequestPriceRows");

            migrationBuilder.DropIndex(
                name: "IX_OrderPriceRows_PriceCalculationChargeId",
                table: "OrderPriceRows");

            migrationBuilder.DropColumn(
                name: "PriceCalculationChargeId",
                table: "RequisitionPriceRows");

            migrationBuilder.DropColumn(
                name: "PriceRowType",
                table: "RequisitionPriceRows");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "RequisitionPriceRows");

            migrationBuilder.DropColumn(
                name: "PriceCalculationChargeId",
                table: "RequestPriceRows");

            migrationBuilder.DropColumn(
                name: "PriceRowType",
                table: "RequestPriceRows");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "RequestPriceRows");

            migrationBuilder.DropColumn(
                name: "PriceCalculationChargeId",
                table: "OrderPriceRows");

            migrationBuilder.DropColumn(
                name: "PriceRowType",
                table: "OrderPriceRows");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "OrderPriceRows");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "RequisitionPriceRows",
                newName: "TotalPrice");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "RequestPriceRows",
                newName: "TotalPrice");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "OrderPriceRows",
                newName: "TotalPrice");

            migrationBuilder.AlterColumn<int>(
                name: "PriceListRowId",
                table: "RequisitionPriceRows",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RequisitionPriceRows",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBrokerFee",
                table: "RequisitionPriceRows",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "PriceListRowId",
                table: "RequestPriceRows",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RequestPriceRows",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBrokerFee",
                table: "RequestPriceRows",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "PriceListRowId",
                table: "OrderPriceRows",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "OrderPriceRows",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBrokerFee",
                table: "OrderPriceRows",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderPriceRows_PriceListRows_PriceListRowId",
                table: "OrderPriceRows",
                column: "PriceListRowId",
                principalTable: "PriceListRows",
                principalColumn: "PriceListRowId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestPriceRows_PriceListRows_PriceListRowId",
                table: "RequestPriceRows",
                column: "PriceListRowId",
                principalTable: "PriceListRows",
                principalColumn: "PriceListRowId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionPriceRows_PriceListRows_PriceListRowId",
                table: "RequisitionPriceRows",
                column: "PriceListRowId",
                principalTable: "PriceListRows",
                principalColumn: "PriceListRowId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
