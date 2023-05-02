using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class Add_Table_PeppolPayload : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PeppolPayloads",
                columns: table => new
                {
                    PeppolPayloadId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdentificationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    RequisitionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ImpersonatingCreatedBy = table.Column<int>(type: "int", nullable: true),
                    ReplacedById = table.Column<int>(type: "int", nullable: true),
                    PeppolMessageType = table.Column<int>(type: "int", nullable: false),
                    OutboundPeppolMessageId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeppolPayloads", x => x.PeppolPayloadId);
                    table.ForeignKey(
                        name: "FK_PeppolPayloads_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeppolPayloads_AspNetUsers_ImpersonatingCreatedBy",
                        column: x => x.ImpersonatingCreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeppolPayloads_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PeppolPayloads_OutboundPeppolMessages_OutboundPeppolMessageId",
                        column: x => x.OutboundPeppolMessageId,
                        principalTable: "OutboundPeppolMessages",
                        principalColumn: "OutboundPeppolMessageId");
                    table.ForeignKey(
                        name: "FK_PeppolPayloads_PeppolPayloads_ReplacedById",
                        column: x => x.ReplacedById,
                        principalTable: "PeppolPayloads",
                        principalColumn: "PeppolPayloadId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeppolPayloads_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeppolPayloads_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "RequisitionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_CreatedBy",
                table: "PeppolPayloads",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_ImpersonatingCreatedBy",
                table: "PeppolPayloads",
                column: "ImpersonatingCreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_OrderId_PeppolMessageType",
                table: "PeppolPayloads",
                columns: new[] { "OrderId", "PeppolMessageType" },
                unique: true,
                filter: "[PeppolMessageType] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_OutboundPeppolMessageId",
                table: "PeppolPayloads",
                column: "OutboundPeppolMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_ReplacedById",
                table: "PeppolPayloads",
                column: "ReplacedById",
                unique: true,
                filter: "[ReplacedById] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_RequestId",
                table: "PeppolPayloads",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_RequisitionId",
                table: "PeppolPayloads",
                column: "RequisitionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PeppolPayloads");
        }
    }
}
