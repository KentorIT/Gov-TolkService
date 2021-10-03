using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderAgreementPayload : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderAgreementPayloads",
                columns: table => new
                {
                    OrderId = table.Column<int>(nullable: false),
                    Index = table.Column<int>(nullable: false),
                    Payload = table.Column<string>(nullable: false),
                    RequisitionId = table.Column<int>(nullable: true),
                    RequestId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAgreementPayloads", x => new { x.OrderId, x.Index });
                    table.ForeignKey(
                        name: "FK_OrderAgreementPayloads_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderAgreementPayloads_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderAgreementPayloads_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "RequisitionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_RequestId",
                table: "OrderAgreementPayloads",
                column: "RequestId",
                unique: true,
                filter: "[RequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_RequisitionId",
                table: "OrderAgreementPayloads",
                column: "RequisitionId",
                unique: true,
                filter: "[RequisitionId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderAgreementPayloads");
        }
    }
}
