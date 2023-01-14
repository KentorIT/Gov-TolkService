using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class removableNotificationPOC : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecipientCustomerUnitId",
                table: "OutboundEmails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecipientUserId",
                table: "OutboundEmails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RequestNotifications",
                columns: table => new
                {
                    RequestNotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    NotificationType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestNotifications", x => x.RequestNotificationId);
                    table.ForeignKey(
                        name: "FK_RequestNotifications_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestNotificationEmail",
                columns: table => new
                {
                    RequestNotificationId = table.Column<int>(type: "int", nullable: false),
                    OutboundEmailId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestNotificationEmail", x => new { x.RequestNotificationId, x.OutboundEmailId });
                    table.ForeignKey(
                        name: "FK_RequestNotificationEmail_OutboundEmails_OutboundEmailId",
                        column: x => x.OutboundEmailId,
                        principalTable: "OutboundEmails",
                        principalColumn: "OutboundEmailId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestNotificationEmail_RequestNotifications_RequestNotificationId",
                        column: x => x.RequestNotificationId,
                        principalTable: "RequestNotifications",
                        principalColumn: "RequestNotificationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestNotificationWebhook",
                columns: table => new
                {
                    RequestNotificationId = table.Column<int>(type: "int", nullable: false),
                    OutboundWebhookId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestNotificationWebhook", x => new { x.RequestNotificationId, x.OutboundWebhookId });
                    table.ForeignKey(
                        name: "FK_RequestNotificationWebhook_OutboundWebHookCalls_OutboundWebhookId",
                        column: x => x.OutboundWebhookId,
                        principalTable: "OutboundWebHookCalls",
                        principalColumn: "OutboundWebHookCallId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestNotificationWebhook_RequestNotifications_RequestNotificationId",
                        column: x => x.RequestNotificationId,
                        principalTable: "RequestNotifications",
                        principalColumn: "RequestNotificationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmails_RecipientUserId",
                table: "OutboundEmails",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestNotificationEmail_OutboundEmailId",
                table: "RequestNotificationEmail",
                column: "OutboundEmailId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestNotifications_RequestId",
                table: "RequestNotifications",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestNotificationWebhook_OutboundWebhookId",
                table: "RequestNotificationWebhook",
                column: "OutboundWebhookId");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboundEmails_AspNetUsers_RecipientUserId",
                table: "OutboundEmails",
                column: "RecipientUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboundEmails_CustomerUnits_RecipientUserId",
                table: "OutboundEmails",
                column: "RecipientUserId",
                principalTable: "CustomerUnits",
                principalColumn: "CustomerUnitId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutboundEmails_AspNetUsers_RecipientUserId",
                table: "OutboundEmails");

            migrationBuilder.DropForeignKey(
                name: "FK_OutboundEmails_CustomerUnits_RecipientUserId",
                table: "OutboundEmails");

            migrationBuilder.DropTable(
                name: "RequestNotificationEmail");

            migrationBuilder.DropTable(
                name: "RequestNotificationWebhook");

            migrationBuilder.DropTable(
                name: "RequestNotifications");

            migrationBuilder.DropIndex(
                name: "IX_OutboundEmails_RecipientUserId",
                table: "OutboundEmails");

            migrationBuilder.DropColumn(
                name: "RecipientCustomerUnitId",
                table: "OutboundEmails");

            migrationBuilder.DropColumn(
                name: "RecipientUserId",
                table: "OutboundEmails");
        }
    }
}
