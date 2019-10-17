using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class WebHookAddFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboundWebHookCalls_ResentHookId",
                table: "OutboundWebHookCalls");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundWebHookCalls_ResentHookId",
                table: "OutboundWebHookCalls",
                column: "ResentHookId",
                unique: true,
                filter: "[ResentHookId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboundWebHookCalls_ResentHookId",
                table: "OutboundWebHookCalls");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundWebHookCalls_ResentHookId",
                table: "OutboundWebHookCalls",
                column: "ResentHookId");
        }
    }
}
