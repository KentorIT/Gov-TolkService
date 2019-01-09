using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OutboundEmailHtml : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Body",
                table: "OutboundEmails",
                newName: "PlainBody");

            migrationBuilder.AddColumn<string>(
                name: "HtmlBody",
                table: "OutboundEmails",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HtmlBody",
                table: "OutboundEmails");

            migrationBuilder.RenameColumn(
                name: "PlainBody",
                table: "OutboundEmails",
                newName: "Body");
        }
    }
}
