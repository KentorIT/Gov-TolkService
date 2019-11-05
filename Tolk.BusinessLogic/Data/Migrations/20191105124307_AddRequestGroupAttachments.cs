using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddRequestGroupAttachments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestGroupAttachments",
                columns: table => new
                {
                    RequestGroupId = table.Column<int>(nullable: false),
                    AttachmentId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestGroupAttachments", x => new { x.RequestGroupId, x.AttachmentId });
                    table.ForeignKey(
                        name: "FK_RequestGroupAttachments_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "AttachmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestGroupAttachments_RequestGroups_RequestGroupId",
                        column: x => x.RequestGroupId,
                        principalTable: "RequestGroups",
                        principalColumn: "RequestGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroupAttachments_AttachmentId",
                table: "RequestGroupAttachments",
                column: "AttachmentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestGroupAttachments");
        }
    }
}
