using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddColumnReplacingEmailToOutboundEmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReplacingEmailId",
                table: "OutboundEmails",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmails_ReplacingEmailId",
                table: "OutboundEmails",
                column: "ReplacingEmailId",
                unique: true,
                filter: "[ReplacingEmailId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboundEmails_OutboundEmails_ReplacingEmailId",
                table: "OutboundEmails",
                column: "ReplacingEmailId",
                principalTable: "OutboundEmails",
                principalColumn: "OutboundEmailId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutboundEmails_OutboundEmails_ReplacingEmailId",
                table: "OutboundEmails");

            migrationBuilder.DropIndex(
                name: "IX_OutboundEmails_ReplacingEmailId",
                table: "OutboundEmails");

            migrationBuilder.DropColumn(
                name: "ReplacingEmailId",
                table: "OutboundEmails");
        }
    }
}
