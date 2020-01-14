using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ColumnOrderGroupAttachmentRemovedNotNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "OrderGroupAttachmentRemoved",
                table: "OrderAttachmentHistoryEntries",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "OrderGroupAttachmentRemoved",
                table: "OrderAttachmentHistoryEntries",
                nullable: true,
                oldClrType: typeof(bool));
        }
    }
}
