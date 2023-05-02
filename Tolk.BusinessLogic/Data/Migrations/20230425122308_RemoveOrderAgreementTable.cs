using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RemoveOrderAgreementTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderAgreementPayloads");

            migrationBuilder.DropIndex(
                name: "IX_PeppolPayloads_OutboundPeppolMessageId",
                table: "PeppolPayloads");

            migrationBuilder.DropIndex(
                name: "IX_PeppolPayloads_RequisitionId",
                table: "PeppolPayloads");

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_OutboundPeppolMessageId",
                table: "PeppolPayloads",
                column: "OutboundPeppolMessageId",
                unique: true,
                filter: "[OutboundPeppolMessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_RequisitionId",
                table: "PeppolPayloads",
                column: "RequisitionId",
                unique: true,
                filter: "[RequisitionId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PeppolPayloads_OutboundPeppolMessageId",
                table: "PeppolPayloads");

            migrationBuilder.DropIndex(
                name: "IX_PeppolPayloads_RequisitionId",
                table: "PeppolPayloads");

            migrationBuilder.CreateTable(
                name: "OrderAgreementPayloads",
                columns: table => new
                {
                    OrderAgreementPayloadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ImpersonatingCreatedBy = table.Column<int>(type: "int", nullable: true),
                    OutboundPeppolMessageId = table.Column<int>(type: "int", nullable: true),
                    ReplacedById = table.Column<int>(type: "int", nullable: true),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    RequisitionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IdentificationNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Index = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAgreementPayloads", x => x.OrderAgreementPayloadId);
                    table.ForeignKey(
                        name: "FK_OrderAgreementPayloads_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderAgreementPayloads_AspNetUsers_ImpersonatingCreatedBy",
                        column: x => x.ImpersonatingCreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderAgreementPayloads_OrderAgreementPayloads_ReplacedById",
                        column: x => x.ReplacedById,
                        principalTable: "OrderAgreementPayloads",
                        principalColumn: "OrderAgreementPayloadId");
                    table.ForeignKey(
                        name: "FK_OrderAgreementPayloads_OutboundPeppolMessages_OutboundPeppolMessageId",
                        column: x => x.OutboundPeppolMessageId,
                        principalTable: "OutboundPeppolMessages",
                        principalColumn: "OutboundPeppolMessageId");
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
                        principalColumn: "RequisitionId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_OutboundPeppolMessageId",
                table: "PeppolPayloads",
                column: "OutboundPeppolMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_RequisitionId",
                table: "PeppolPayloads",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_CreatedBy",
                table: "OrderAgreementPayloads",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_ImpersonatingCreatedBy",
                table: "OrderAgreementPayloads",
                column: "ImpersonatingCreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_OutboundPeppolMessageId",
                table: "OrderAgreementPayloads",
                column: "OutboundPeppolMessageId",
                unique: true,
                filter: "[OutboundPeppolMessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_ReplacedById",
                table: "OrderAgreementPayloads",
                column: "ReplacedById",
                unique: true,
                filter: "[ReplacedById] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_RequestId_Index",
                table: "OrderAgreementPayloads",
                columns: new[] { "RequestId", "Index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_RequisitionId",
                table: "OrderAgreementPayloads",
                column: "RequisitionId",
                unique: true,
                filter: "[RequisitionId] IS NOT NULL");
        }
    }
}
