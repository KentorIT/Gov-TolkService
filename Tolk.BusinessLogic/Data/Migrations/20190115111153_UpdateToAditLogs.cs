using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class UpdateToAditLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaimHistoryEntries_AspNetUsers_UserId",
                table: "AspNetUserClaimHistoryEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserHistoryEntries_AspNetUsers_UserId",
                table: "AspNetUserHistoryEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoleHistoryEntries_AspNetUsers_UserId",
                table: "AspNetUserRoleHistoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoleHistoryEntries_UserId",
                table: "AspNetUserRoleHistoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserHistoryEntries_UserId",
                table: "AspNetUserHistoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserClaimHistoryEntries_UserId",
                table: "AspNetUserClaimHistoryEntries");

            migrationBuilder.DropColumn(
                name: "UserAuditLogEntry",
                table: "AspNetUserRoleHistoryEntries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AspNetUserRoleHistoryEntries");

            migrationBuilder.DropColumn(
                name: "UserAuditLogEntry",
                table: "AspNetUserHistoryEntries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AspNetUserHistoryEntries");

            migrationBuilder.DropColumn(
                name: "UserAuditLogEntry",
                table: "AspNetUserClaimHistoryEntries");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AspNetUserClaimHistoryEntries");

            migrationBuilder.CreateTable(
                name: "UserNotificationSettingHistoryEntries",
                columns: table => new
                {
                    UserNotificationSettingHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserAuditLogEntryId = table.Column<int>(nullable: false),
                    NotificationChannel = table.Column<int>(nullable: false),
                    NotificationType = table.Column<int>(nullable: false),
                    ConnectionInformation = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationSettingHistoryEntries", x => x.UserNotificationSettingHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_UserNotificationSettingHistoryEntries_UserAuditLogEntries_UserAuditLogEntryId",
                        column: x => x.UserAuditLogEntryId,
                        principalTable: "UserAuditLogEntries",
                        principalColumn: "UserAuditLogEntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoleHistoryEntries_UserAuditLogEntryId",
                table: "AspNetUserRoleHistoryEntries",
                column: "UserAuditLogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserHistoryEntries_UserAuditLogEntryId",
                table: "AspNetUserHistoryEntries",
                column: "UserAuditLogEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaimHistoryEntries_UserAuditLogEntryId",
                table: "AspNetUserClaimHistoryEntries",
                column: "UserAuditLogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationSettingHistoryEntries_UserAuditLogEntryId",
                table: "UserNotificationSettingHistoryEntries",
                column: "UserAuditLogEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaimHistoryEntries_UserAuditLogEntries_UserAuditLogEntryId",
                table: "AspNetUserClaimHistoryEntries",
                column: "UserAuditLogEntryId",
                principalTable: "UserAuditLogEntries",
                principalColumn: "UserAuditLogEntryId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserHistoryEntries_UserAuditLogEntries_UserAuditLogEntryId",
                table: "AspNetUserHistoryEntries",
                column: "UserAuditLogEntryId",
                principalTable: "UserAuditLogEntries",
                principalColumn: "UserAuditLogEntryId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoleHistoryEntries_UserAuditLogEntries_UserAuditLogEntryId",
                table: "AspNetUserRoleHistoryEntries",
                column: "UserAuditLogEntryId",
                principalTable: "UserAuditLogEntries",
                principalColumn: "UserAuditLogEntryId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaimHistoryEntries_UserAuditLogEntries_UserAuditLogEntryId",
                table: "AspNetUserClaimHistoryEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserHistoryEntries_UserAuditLogEntries_UserAuditLogEntryId",
                table: "AspNetUserHistoryEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoleHistoryEntries_UserAuditLogEntries_UserAuditLogEntryId",
                table: "AspNetUserRoleHistoryEntries");

            migrationBuilder.DropTable(
                name: "UserNotificationSettingHistoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserRoleHistoryEntries_UserAuditLogEntryId",
                table: "AspNetUserRoleHistoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserHistoryEntries_UserAuditLogEntryId",
                table: "AspNetUserHistoryEntries");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserClaimHistoryEntries_UserAuditLogEntryId",
                table: "AspNetUserClaimHistoryEntries");

            migrationBuilder.AddColumn<int>(
                name: "UserAuditLogEntry",
                table: "AspNetUserRoleHistoryEntries",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "AspNetUserRoleHistoryEntries",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserAuditLogEntry",
                table: "AspNetUserHistoryEntries",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "AspNetUserHistoryEntries",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserAuditLogEntry",
                table: "AspNetUserClaimHistoryEntries",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "AspNetUserClaimHistoryEntries",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoleHistoryEntries_UserId",
                table: "AspNetUserRoleHistoryEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserHistoryEntries_UserId",
                table: "AspNetUserHistoryEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaimHistoryEntries_UserId",
                table: "AspNetUserClaimHistoryEntries",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaimHistoryEntries_AspNetUsers_UserId",
                table: "AspNetUserClaimHistoryEntries",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserHistoryEntries_AspNetUsers_UserId",
                table: "AspNetUserHistoryEntries",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoleHistoryEntries_AspNetUsers_UserId",
                table: "AspNetUserRoleHistoryEntries",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
