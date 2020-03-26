using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddTableUserDefaultSettingOrderRequirementHistoryEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDefaultSettingsOrderRequirementHistoryEntries",
                columns: table => new
                {
                    UserDefaultSettingsOrderRequirementHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserAuditLogEntryId = table.Column<int>(nullable: false),
                    RequirementType = table.Column<int>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    IsRequired = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDefaultSettingsOrderRequirementHistoryEntries", x => x.UserDefaultSettingsOrderRequirementHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_UserDefaultSettingsOrderRequirementHistoryEntries_UserAuditLogEntries_UserAuditLogEntryId",
                        column: x => x.UserAuditLogEntryId,
                        principalTable: "UserAuditLogEntries",
                        principalColumn: "UserAuditLogEntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDefaultSettingsOrderRequirementHistoryEntries_UserAuditLogEntryId",
                table: "UserDefaultSettingsOrderRequirementHistoryEntries",
                column: "UserAuditLogEntryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDefaultSettingsOrderRequirementHistoryEntries");
        }
    }
}
