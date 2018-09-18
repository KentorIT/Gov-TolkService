using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class Remove_IX_TemporaryAttachmentGroups_AttachmentId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropIndex(
                name: "IX_TemporaryAttachmentGroups_AttachmentId",
                table: "TemporaryAttachmentGroups",
                schema: "dbo");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryAttachmentGroups_AttachmentId",
                table: "TemporaryAttachmentGroups",
                column: "AttachmentId",
                unique: true);
        }
    }
}
