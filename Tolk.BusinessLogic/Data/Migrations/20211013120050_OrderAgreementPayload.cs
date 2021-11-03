using Microsoft.EntityFrameworkCore.Migrations;
using System;

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
                    RequestId = table.Column<int>(nullable: false),
                    Index = table.Column<int>(nullable: false),
                    Payload = table.Column<byte[]>(nullable: false),
                    RequisitionId = table.Column<int>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    CreatedBy = table.Column<int>(nullable: true),
                    ImpersonatingCreatedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAgreementPayloads", x => new { x.RequestId, x.Index });
                    table.ForeignKey(
                        name: "FK_OrderAgreementPayloads_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderAgreementPayloads_AspNetUsers_ImpersonatingCreatedBy",
                        column: x => x.ImpersonatingCreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderAgreementPayloads_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderAgreementPayloads_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "RequisitionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_CreatedBy",
                table: "OrderAgreementPayloads",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_ImpersonatingCreatedBy",
                table: "OrderAgreementPayloads",
                column: "ImpersonatingCreatedBy");

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
