using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddResentByUserToOutboundEmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResentByUserId",
                table: "OutboundEmails",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmails_ResentByUserId",
                table: "OutboundEmails",
                column: "ResentByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboundEmails_AspNetUsers_ResentByUserId",
                table: "OutboundEmails",
                column: "ResentByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutboundEmails_AspNetUsers_ResentByUserId",
                table: "OutboundEmails");

            migrationBuilder.DropIndex(
                name: "IX_OutboundEmails_ResentByUserId",
                table: "OutboundEmails");

            migrationBuilder.DropColumn(
                name: "ResentByUserId",
                table: "OutboundEmails");
        }
    }
}
