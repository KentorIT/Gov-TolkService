using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddOrderAttachmentHistoryEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderAttachmentHistoryEntries",
                columns: table => new
                {
                    OrderAttachmentHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    OrderChangeLogEntryId = table.Column<int>(nullable: false),
                    AttachmentId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAttachmentHistoryEntries", x => x.OrderAttachmentHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_OrderAttachmentHistoryEntries_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "AttachmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderAttachmentHistoryEntries_OrderChangeLogEntries_OrderChangeLogEntryId",
                        column: x => x.OrderChangeLogEntryId,
                        principalTable: "OrderChangeLogEntries",
                        principalColumn: "OrderChangeLogEntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderAttachmentHistoryEntries_AttachmentId",
                table: "OrderAttachmentHistoryEntries",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAttachmentHistoryEntries_OrderChangeLogEntryId",
                table: "OrderAttachmentHistoryEntries",
                column: "OrderChangeLogEntryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderAttachmentHistoryEntries");
        }
    }
}
