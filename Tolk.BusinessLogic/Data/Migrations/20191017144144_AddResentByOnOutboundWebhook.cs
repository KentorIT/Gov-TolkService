using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddResentByOnOutboundWebhook : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResentImpersonatorUserId",
                table: "OutboundWebHookCalls",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResentUserId",
                table: "OutboundWebHookCalls",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboundWebHookCalls_ResentImpersonatorUserId",
                table: "OutboundWebHookCalls",
                column: "ResentImpersonatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundWebHookCalls_ResentUserId",
                table: "OutboundWebHookCalls",
                column: "ResentUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboundWebHookCalls_AspNetUsers_ResentImpersonatorUserId",
                table: "OutboundWebHookCalls",
                column: "ResentImpersonatorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OutboundWebHookCalls_AspNetUsers_ResentUserId",
                table: "OutboundWebHookCalls",
                column: "ResentUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutboundWebHookCalls_AspNetUsers_ResentImpersonatorUserId",
                table: "OutboundWebHookCalls");

            migrationBuilder.DropForeignKey(
                name: "FK_OutboundWebHookCalls_AspNetUsers_ResentUserId",
                table: "OutboundWebHookCalls");

            migrationBuilder.DropIndex(
                name: "IX_OutboundWebHookCalls_ResentImpersonatorUserId",
                table: "OutboundWebHookCalls");

            migrationBuilder.DropIndex(
                name: "IX_OutboundWebHookCalls_ResentUserId",
                table: "OutboundWebHookCalls");

            migrationBuilder.DropColumn(
                name: "ResentImpersonatorUserId",
                table: "OutboundWebHookCalls");

            migrationBuilder.DropColumn(
                name: "ResentUserId",
                table: "OutboundWebHookCalls");
        }
    }
}
