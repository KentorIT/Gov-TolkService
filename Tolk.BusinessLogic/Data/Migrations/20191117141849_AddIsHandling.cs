using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddIsHandling : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasNotifiedFailure",
                table: "OutboundWebHookCalls",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHandling",
                table: "OutboundWebHookCalls",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHandling",
                table: "OutboundEmails",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasNotifiedFailure",
                table: "OutboundWebHookCalls");

            migrationBuilder.DropColumn(
                name: "IsHandling",
                table: "OutboundWebHookCalls");

            migrationBuilder.DropColumn(
                name: "IsHandling",
                table: "OutboundEmails");
        }
    }
}
