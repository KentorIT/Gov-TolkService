using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class UserDefaultSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDefaultSettingHistoryEntries",
                columns: table => new
                {
                    UserDefaultSettingHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserAuditLogEntryId = table.Column<int>(nullable: false),
                    DefaultSettingType = table.Column<int>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDefaultSettingHistoryEntries", x => x.UserDefaultSettingHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_UserDefaultSettingHistoryEntries_UserAuditLogEntries_UserAuditLogEntryId",
                        column: x => x.UserAuditLogEntryId,
                        principalTable: "UserAuditLogEntries",
                        principalColumn: "UserAuditLogEntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDefaultSettings",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    DefaultSettingType = table.Column<int>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDefaultSettings", x => new { x.UserId, x.DefaultSettingType });
                    table.ForeignKey(
                        name: "FK_UserDefaultSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDefaultSettingHistoryEntries_UserAuditLogEntryId",
                table: "UserDefaultSettingHistoryEntries",
                column: "UserAuditLogEntryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDefaultSettingHistoryEntries");

            migrationBuilder.DropTable(
                name: "UserDefaultSettings");
        }
    }
}
