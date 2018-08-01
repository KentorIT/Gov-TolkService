using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class PluralizePriceRowTableNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderPriceRow_Orders_OrderId",
                table: "OrderPriceRow");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderPriceRow_PriceListRows_PriceListRowId",
                table: "OrderPriceRow");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestPriceRow_PriceListRows_PriceListRowId",
                table: "RequestPriceRow");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestPriceRow_Requests_RequestId",
                table: "RequestPriceRow");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionPriceRow_PriceListRows_PriceListRowId",
                table: "RequisitionPriceRow");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionPriceRow_Requisitions_RequisitionId",
                table: "RequisitionPriceRow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequisitionPriceRow",
                table: "RequisitionPriceRow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestPriceRow",
                table: "RequestPriceRow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderPriceRow",
                table: "OrderPriceRow");

            migrationBuilder.RenameTable(
                name: "RequisitionPriceRow",
                newName: "RequisitionPriceRows");

            migrationBuilder.RenameTable(
                name: "RequestPriceRow",
                newName: "RequestPriceRows");

            migrationBuilder.RenameTable(
                name: "OrderPriceRow",
                newName: "OrderPriceRows");

            migrationBuilder.RenameIndex(
                name: "IX_RequisitionPriceRow_RequisitionId",
                table: "RequisitionPriceRows",
                newName: "IX_RequisitionPriceRows_RequisitionId");

            migrationBuilder.RenameIndex(
                name: "IX_RequisitionPriceRow_PriceListRowId",
                table: "RequisitionPriceRows",
                newName: "IX_RequisitionPriceRows_PriceListRowId");

            migrationBuilder.RenameIndex(
                name: "IX_RequestPriceRow_RequestId",
                table: "RequestPriceRows",
                newName: "IX_RequestPriceRows_RequestId");

            migrationBuilder.RenameIndex(
                name: "IX_RequestPriceRow_PriceListRowId",
                table: "RequestPriceRows",
                newName: "IX_RequestPriceRows_PriceListRowId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderPriceRow_PriceListRowId",
                table: "OrderPriceRows",
                newName: "IX_OrderPriceRows_PriceListRowId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderPriceRow_OrderId",
                table: "OrderPriceRows",
                newName: "IX_OrderPriceRows_OrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequisitionPriceRows",
                table: "RequisitionPriceRows",
                column: "RequisitionPriceRowId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestPriceRows",
                table: "RequestPriceRows",
                column: "RequestPriceRowId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderPriceRows",
                table: "OrderPriceRows",
                column: "OrderPriceRowId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderPriceRows_Orders_OrderId",
                table: "OrderPriceRows",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_RequestPriceRows_Requests_RequestId",
                table: "RequestPriceRows",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionPriceRows_PriceListRows_PriceListRowId",
                table: "RequisitionPriceRows",
                column: "PriceListRowId",
                principalTable: "PriceListRows",
                principalColumn: "PriceListRowId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionPriceRows_Requisitions_RequisitionId",
                table: "RequisitionPriceRows",
                column: "RequisitionId",
                principalTable: "Requisitions",
                principalColumn: "RequisitionId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderPriceRows_Orders_OrderId",
                table: "OrderPriceRows");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderPriceRows_PriceListRows_PriceListRowId",
                table: "OrderPriceRows");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestPriceRows_PriceListRows_PriceListRowId",
                table: "RequestPriceRows");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestPriceRows_Requests_RequestId",
                table: "RequestPriceRows");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionPriceRows_PriceListRows_PriceListRowId",
                table: "RequisitionPriceRows");

            migrationBuilder.DropForeignKey(
                name: "FK_RequisitionPriceRows_Requisitions_RequisitionId",
                table: "RequisitionPriceRows");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequisitionPriceRows",
                table: "RequisitionPriceRows");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestPriceRows",
                table: "RequestPriceRows");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderPriceRows",
                table: "OrderPriceRows");

            migrationBuilder.RenameTable(
                name: "RequisitionPriceRows",
                newName: "RequisitionPriceRow");

            migrationBuilder.RenameTable(
                name: "RequestPriceRows",
                newName: "RequestPriceRow");

            migrationBuilder.RenameTable(
                name: "OrderPriceRows",
                newName: "OrderPriceRow");

            migrationBuilder.RenameIndex(
                name: "IX_RequisitionPriceRows_RequisitionId",
                table: "RequisitionPriceRow",
                newName: "IX_RequisitionPriceRow_RequisitionId");

            migrationBuilder.RenameIndex(
                name: "IX_RequisitionPriceRows_PriceListRowId",
                table: "RequisitionPriceRow",
                newName: "IX_RequisitionPriceRow_PriceListRowId");

            migrationBuilder.RenameIndex(
                name: "IX_RequestPriceRows_RequestId",
                table: "RequestPriceRow",
                newName: "IX_RequestPriceRow_RequestId");

            migrationBuilder.RenameIndex(
                name: "IX_RequestPriceRows_PriceListRowId",
                table: "RequestPriceRow",
                newName: "IX_RequestPriceRow_PriceListRowId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderPriceRows_PriceListRowId",
                table: "OrderPriceRow",
                newName: "IX_OrderPriceRow_PriceListRowId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderPriceRows_OrderId",
                table: "OrderPriceRow",
                newName: "IX_OrderPriceRow_OrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequisitionPriceRow",
                table: "RequisitionPriceRow",
                column: "RequisitionPriceRowId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestPriceRow",
                table: "RequestPriceRow",
                column: "RequestPriceRowId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderPriceRow",
                table: "OrderPriceRow",
                column: "OrderPriceRowId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderPriceRow_Orders_OrderId",
                table: "OrderPriceRow",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderPriceRow_PriceListRows_PriceListRowId",
                table: "OrderPriceRow",
                column: "PriceListRowId",
                principalTable: "PriceListRows",
                principalColumn: "PriceListRowId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestPriceRow_PriceListRows_PriceListRowId",
                table: "RequestPriceRow",
                column: "PriceListRowId",
                principalTable: "PriceListRows",
                principalColumn: "PriceListRowId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestPriceRow_Requests_RequestId",
                table: "RequestPriceRow",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionPriceRow_PriceListRows_PriceListRowId",
                table: "RequisitionPriceRow",
                column: "PriceListRowId",
                principalTable: "PriceListRows",
                principalColumn: "PriceListRowId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequisitionPriceRow_Requisitions_RequisitionId",
                table: "RequisitionPriceRow",
                column: "RequisitionId",
                principalTable: "Requisitions",
                principalColumn: "RequisitionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
