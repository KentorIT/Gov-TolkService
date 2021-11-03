using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class TemporaryAttachmentGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemporaryAttachmentGroups",
                columns: table => new
                {
                    TemporaryAttachmentGroupKey = table.Column<Guid>(nullable: false),
                    AttachmentId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryAttachmentGroups", x => new { x.TemporaryAttachmentGroupKey, x.AttachmentId });
                    table.ForeignKey(
                        name: "FK_TemporaryAttachmentGroups_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "AttachmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryAttachmentGroups_AttachmentId",
                table: "TemporaryAttachmentGroups",
                column: "AttachmentId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemporaryAttachmentGroups");
        }
    }
}
