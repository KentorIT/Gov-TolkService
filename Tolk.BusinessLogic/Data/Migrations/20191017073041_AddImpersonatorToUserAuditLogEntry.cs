using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddImpersonatorToUserAuditLogEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UpdatedByImpersonatorId",
                table: "UserAuditLogEntries",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLogEntries_UpdatedByImpersonatorId",
                table: "UserAuditLogEntries",
                column: "UpdatedByImpersonatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAuditLogEntries_AspNetUsers_UpdatedByImpersonatorId",
                table: "UserAuditLogEntries",
                column: "UpdatedByImpersonatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAuditLogEntries_AspNetUsers_UpdatedByImpersonatorId",
                table: "UserAuditLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_UserAuditLogEntries_UpdatedByImpersonatorId",
                table: "UserAuditLogEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedByImpersonatorId",
                table: "UserAuditLogEntries");
        }
    }
}
