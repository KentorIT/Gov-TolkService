using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RenameOrderAttachment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderAttachment_Attachments_AttachmentId",
                table: "OrderAttachment");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderAttachment_Orders_OrderId",
                table: "OrderAttachment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderAttachment",
                table: "OrderAttachment");

            migrationBuilder.RenameTable(
                name: "OrderAttachment",
                newName: "OrderAttachments");

            migrationBuilder.RenameIndex(
                name: "IX_OrderAttachment_AttachmentId",
                table: "OrderAttachments",
                newName: "IX_OrderAttachments_AttachmentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderAttachments",
                table: "OrderAttachments",
                columns: new[] { "OrderId", "AttachmentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAttachments_Attachments_AttachmentId",
                table: "OrderAttachments",
                column: "AttachmentId",
                principalTable: "Attachments",
                principalColumn: "AttachmentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAttachments_Orders_OrderId",
                table: "OrderAttachments",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderAttachments_Attachments_AttachmentId",
                table: "OrderAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderAttachments_Orders_OrderId",
                table: "OrderAttachments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderAttachments",
                table: "OrderAttachments");

            migrationBuilder.RenameTable(
                name: "OrderAttachments",
                newName: "OrderAttachment");

            migrationBuilder.RenameIndex(
                name: "IX_OrderAttachments_AttachmentId",
                table: "OrderAttachment",
                newName: "IX_OrderAttachment_AttachmentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderAttachment",
                table: "OrderAttachment",
                columns: new[] { "OrderId", "AttachmentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAttachment_Attachments_AttachmentId",
                table: "OrderAttachment",
                column: "AttachmentId",
                principalTable: "Attachments",
                principalColumn: "AttachmentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAttachment_Orders_OrderId",
                table: "OrderAttachment",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
