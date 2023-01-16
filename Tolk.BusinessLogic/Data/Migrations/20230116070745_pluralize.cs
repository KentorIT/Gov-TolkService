using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class pluralize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestNotificationEmail_OutboundEmails_OutboundEmailId",
                table: "RequestNotificationEmail");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestNotificationEmail_RequestNotifications_RequestNotificationId",
                table: "RequestNotificationEmail");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestNotificationWebhook_OutboundWebHookCalls_OutboundWebhookId",
                table: "RequestNotificationWebhook");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestNotificationWebhook_RequestNotifications_RequestNotificationId",
                table: "RequestNotificationWebhook");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestNotificationWebhook",
                table: "RequestNotificationWebhook");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestNotificationEmail",
                table: "RequestNotificationEmail");

            migrationBuilder.RenameTable(
                name: "RequestNotificationWebhook",
                newName: "RequestNotificationWebhooks");

            migrationBuilder.RenameTable(
                name: "RequestNotificationEmail",
                newName: "RequestNotificationEmails");

            migrationBuilder.RenameIndex(
                name: "IX_RequestNotificationWebhook_OutboundWebhookId",
                table: "RequestNotificationWebhooks",
                newName: "IX_RequestNotificationWebhooks_OutboundWebhookId");

            migrationBuilder.RenameIndex(
                name: "IX_RequestNotificationEmail_OutboundEmailId",
                table: "RequestNotificationEmails",
                newName: "IX_RequestNotificationEmails_OutboundEmailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestNotificationWebhooks",
                table: "RequestNotificationWebhooks",
                columns: new[] { "RequestNotificationId", "OutboundWebhookId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestNotificationEmails",
                table: "RequestNotificationEmails",
                columns: new[] { "RequestNotificationId", "OutboundEmailId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RequestNotificationEmails_OutboundEmails_OutboundEmailId",
                table: "RequestNotificationEmails",
                column: "OutboundEmailId",
                principalTable: "OutboundEmails",
                principalColumn: "OutboundEmailId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestNotificationEmails_RequestNotifications_RequestNotificationId",
                table: "RequestNotificationEmails",
                column: "RequestNotificationId",
                principalTable: "RequestNotifications",
                principalColumn: "RequestNotificationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestNotificationWebhooks_OutboundWebHookCalls_OutboundWebhookId",
                table: "RequestNotificationWebhooks",
                column: "OutboundWebhookId",
                principalTable: "OutboundWebHookCalls",
                principalColumn: "OutboundWebHookCallId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestNotificationWebhooks_RequestNotifications_RequestNotificationId",
                table: "RequestNotificationWebhooks",
                column: "RequestNotificationId",
                principalTable: "RequestNotifications",
                principalColumn: "RequestNotificationId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestNotificationEmails_OutboundEmails_OutboundEmailId",
                table: "RequestNotificationEmails");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestNotificationEmails_RequestNotifications_RequestNotificationId",
                table: "RequestNotificationEmails");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestNotificationWebhooks_OutboundWebHookCalls_OutboundWebhookId",
                table: "RequestNotificationWebhooks");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestNotificationWebhooks_RequestNotifications_RequestNotificationId",
                table: "RequestNotificationWebhooks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestNotificationWebhooks",
                table: "RequestNotificationWebhooks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestNotificationEmails",
                table: "RequestNotificationEmails");

            migrationBuilder.RenameTable(
                name: "RequestNotificationWebhooks",
                newName: "RequestNotificationWebhook");

            migrationBuilder.RenameTable(
                name: "RequestNotificationEmails",
                newName: "RequestNotificationEmail");

            migrationBuilder.RenameIndex(
                name: "IX_RequestNotificationWebhooks_OutboundWebhookId",
                table: "RequestNotificationWebhook",
                newName: "IX_RequestNotificationWebhook_OutboundWebhookId");

            migrationBuilder.RenameIndex(
                name: "IX_RequestNotificationEmails_OutboundEmailId",
                table: "RequestNotificationEmail",
                newName: "IX_RequestNotificationEmail_OutboundEmailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestNotificationWebhook",
                table: "RequestNotificationWebhook",
                columns: new[] { "RequestNotificationId", "OutboundWebhookId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestNotificationEmail",
                table: "RequestNotificationEmail",
                columns: new[] { "RequestNotificationId", "OutboundEmailId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RequestNotificationEmail_OutboundEmails_OutboundEmailId",
                table: "RequestNotificationEmail",
                column: "OutboundEmailId",
                principalTable: "OutboundEmails",
                principalColumn: "OutboundEmailId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestNotificationEmail_RequestNotifications_RequestNotificationId",
                table: "RequestNotificationEmail",
                column: "RequestNotificationId",
                principalTable: "RequestNotifications",
                principalColumn: "RequestNotificationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestNotificationWebhook_OutboundWebHookCalls_OutboundWebhookId",
                table: "RequestNotificationWebhook",
                column: "OutboundWebhookId",
                principalTable: "OutboundWebHookCalls",
                principalColumn: "OutboundWebHookCallId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestNotificationWebhook_RequestNotifications_RequestNotificationId",
                table: "RequestNotificationWebhook",
                column: "RequestNotificationId",
                principalTable: "RequestNotifications",
                principalColumn: "RequestNotificationId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
