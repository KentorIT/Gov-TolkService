using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ImprovedPriceRows : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RequisitionPriceRow",
                table: "RequisitionPriceRow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestPriceRow",
                table: "RequestPriceRow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderPriceRow",
                table: "OrderPriceRow");

            migrationBuilder.AddColumn<int>(
                name: "RequisitionPriceRowId",
                table: "RequisitionPriceRow",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsBrokerFee",
                table: "RequisitionPriceRow",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RequestPriceRowId",
                table: "RequestPriceRow",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsBrokerFee",
                table: "RequestPriceRow",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OrderPriceRowId",
                table: "OrderPriceRow",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsBrokerFee",
                table: "OrderPriceRow",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionPriceRow_RequisitionId",
                table: "RequisitionPriceRow",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestPriceRow_RequestId",
                table: "RequestPriceRow",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPriceRow_OrderId",
                table: "OrderPriceRow",
                column: "OrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RequisitionPriceRow",
                table: "RequisitionPriceRow");

            migrationBuilder.DropIndex(
                name: "IX_RequisitionPriceRow_RequisitionId",
                table: "RequisitionPriceRow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestPriceRow",
                table: "RequestPriceRow");

            migrationBuilder.DropIndex(
                name: "IX_RequestPriceRow_RequestId",
                table: "RequestPriceRow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderPriceRow",
                table: "OrderPriceRow");

            migrationBuilder.DropIndex(
                name: "IX_OrderPriceRow_OrderId",
                table: "OrderPriceRow");

            migrationBuilder.DropColumn(
                name: "RequisitionPriceRowId",
                table: "RequisitionPriceRow");

            migrationBuilder.DropColumn(
                name: "IsBrokerFee",
                table: "RequisitionPriceRow");

            migrationBuilder.DropColumn(
                name: "RequestPriceRowId",
                table: "RequestPriceRow");

            migrationBuilder.DropColumn(
                name: "IsBrokerFee",
                table: "RequestPriceRow");

            migrationBuilder.DropColumn(
                name: "OrderPriceRowId",
                table: "OrderPriceRow");

            migrationBuilder.DropColumn(
                name: "IsBrokerFee",
                table: "OrderPriceRow");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequisitionPriceRow",
                table: "RequisitionPriceRow",
                columns: new[] { "RequisitionId", "PriceListRowId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestPriceRow",
                table: "RequestPriceRow",
                columns: new[] { "RequestId", "PriceListRowId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderPriceRow",
                table: "OrderPriceRow",
                columns: new[] { "OrderId", "PriceListRowId" });
        }
    }
}
