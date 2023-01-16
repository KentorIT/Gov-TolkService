using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class BadFkPointerInOutboundEmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutboundEmails_CustomerUnits_RecipientUserId",
                table: "OutboundEmails");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundEmails_RecipientCustomerUnitId",
                table: "OutboundEmails",
                column: "RecipientCustomerUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboundEmails_CustomerUnits_RecipientCustomerUnitId",
                table: "OutboundEmails",
                column: "RecipientCustomerUnitId",
                principalTable: "CustomerUnits",
                principalColumn: "CustomerUnitId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutboundEmails_CustomerUnits_RecipientCustomerUnitId",
                table: "OutboundEmails");

            migrationBuilder.DropIndex(
                name: "IX_OutboundEmails_RecipientCustomerUnitId",
                table: "OutboundEmails");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboundEmails_CustomerUnits_RecipientUserId",
                table: "OutboundEmails",
                column: "RecipientUserId",
                principalTable: "CustomerUnits",
                principalColumn: "CustomerUnitId");
        }
    }
}
