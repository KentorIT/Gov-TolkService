using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ChangeOrderAttachmentHistoryEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OrderGroupAttachmentRemoved",
                table: "OrderAttachmentHistoryEntries",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderGroupAttachmentRemoved",
                table: "OrderAttachmentHistoryEntries");
        }
    }
}
