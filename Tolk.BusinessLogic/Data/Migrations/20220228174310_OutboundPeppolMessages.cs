using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OutboundPeppolMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OutboundPeppolMessageId",
                table: "OrderAgreementPayloads",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OutboundPeppolMessages",
                columns: table => new
                {
                    OutboundPeppolMessageId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveredAt = table.Column<DateTimeOffset>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    IsHandling = table.Column<bool>(nullable: false),
                    Identifier = table.Column<string>(nullable: false),
                    Recipient = table.Column<string>(nullable: false),
                    Payload = table.Column<byte[]>(nullable: false),
                    ReplacingPeppolMessageId = table.Column<int>(nullable: true),
                    ResentByUserId = table.Column<int>(nullable: true),
                    NotificationType = table.Column<int>(nullable: false),
                    ResentImpersonatorUserId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundPeppolMessages", x => x.OutboundPeppolMessageId);
                    table.ForeignKey(
                        name: "FK_OutboundPeppolMessages_OutboundPeppolMessages_ReplacingPeppolMessageId",
                        column: x => x.ReplacingPeppolMessageId,
                        principalTable: "OutboundPeppolMessages",
                        principalColumn: "OutboundPeppolMessageId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OutboundPeppolMessages_AspNetUsers_ResentByUserId",
                        column: x => x.ResentByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OutboundPeppolMessages_AspNetUsers_ResentImpersonatorUserId",
                        column: x => x.ResentImpersonatorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_OutboundPeppolMessageId",
                table: "OrderAgreementPayloads",
                column: "OutboundPeppolMessageId",
                unique: true,
                filter: "[OutboundPeppolMessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundPeppolMessages_ReplacingPeppolMessageId",
                table: "OutboundPeppolMessages",
                column: "ReplacingPeppolMessageId",
                unique: true,
                filter: "[ReplacingPeppolMessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundPeppolMessages_ResentByUserId",
                table: "OutboundPeppolMessages",
                column: "ResentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundPeppolMessages_ResentImpersonatorUserId",
                table: "OutboundPeppolMessages",
                column: "ResentImpersonatorUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAgreementPayloads_OutboundPeppolMessages_OutboundPeppolMessageId",
                table: "OrderAgreementPayloads",
                column: "OutboundPeppolMessageId",
                principalTable: "OutboundPeppolMessages",
                principalColumn: "OutboundPeppolMessageId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderAgreementPayloads_OutboundPeppolMessages_OutboundPeppolMessageId",
                table: "OrderAgreementPayloads");

            migrationBuilder.DropTable(
                name: "OutboundPeppolMessages");

            migrationBuilder.DropIndex(
                name: "IX_OrderAgreementPayloads_OutboundPeppolMessageId",
                table: "OrderAgreementPayloads");

            migrationBuilder.DropColumn(
                name: "OutboundPeppolMessageId",
                table: "OrderAgreementPayloads");
        }
    }
}
