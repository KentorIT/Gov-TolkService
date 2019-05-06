using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddCustomerUnitUserHistoryEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerUnitUserHistoryEntries",
                columns: table => new
                {
                    CustomerUnitUserHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserAuditLogEntryId = table.Column<int>(nullable: false),
                    CustomerUnitId = table.Column<int>(nullable: false),
                    IsLocalAdmin = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerUnitUserHistoryEntries", x => x.CustomerUnitUserHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_CustomerUnitUserHistoryEntries_UserAuditLogEntries_UserAuditLogEntryId",
                        column: x => x.UserAuditLogEntryId,
                        principalTable: "UserAuditLogEntries",
                        principalColumn: "UserAuditLogEntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUnitUserHistoryEntries_UserAuditLogEntryId",
                table: "CustomerUnitUserHistoryEntries",
                column: "UserAuditLogEntryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerUnitUserHistoryEntries");
        }
    }
}
