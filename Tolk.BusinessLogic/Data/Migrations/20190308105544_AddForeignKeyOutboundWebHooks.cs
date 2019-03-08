using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddForeignKeyOutboundWebHooks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OutboundWebHookCalls_ResentHookId",
                table: "OutboundWebHookCalls",
                column: "ResentHookId");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboundWebHookCalls_OutboundWebHookCalls_ResentHookId",
                table: "OutboundWebHookCalls",
                column: "ResentHookId",
                principalTable: "OutboundWebHookCalls",
                principalColumn: "OutboundWebHookCallId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutboundWebHookCalls_OutboundWebHookCalls_ResentHookId",
                table: "OutboundWebHookCalls");

            migrationBuilder.DropIndex(
                name: "IX_OutboundWebHookCalls_ResentHookId",
                table: "OutboundWebHookCalls");
        }
    }
}
