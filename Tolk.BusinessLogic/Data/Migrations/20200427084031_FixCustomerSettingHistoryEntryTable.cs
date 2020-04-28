using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class FixCustomerSettingHistoryEntryTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerHistoryEntries");

            migrationBuilder.CreateTable(
                name: "CustomerSettingHistoryEntries",
                columns: table => new
                {
                    CustomerSettingHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CustomerChangeLogEntryId = table.Column<int>(nullable: false),
                    CustomerSettingType = table.Column<int>(nullable: false),
                    Value = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSettingHistoryEntries", x => x.CustomerSettingHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_CustomerSettingHistoryEntries_CustomerChangeLogEntries_CustomerChangeLogEntryId",
                        column: x => x.CustomerChangeLogEntryId,
                        principalTable: "CustomerChangeLogEntries",
                        principalColumn: "CustomerChangeLogEntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSettingHistoryEntries_CustomerChangeLogEntryId",
                table: "CustomerSettingHistoryEntries",
                column: "CustomerChangeLogEntryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSettingHistoryEntries");

            migrationBuilder.CreateTable(
                name: "CustomerHistoryEntries",
                columns: table => new
                {
                    CustomerHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ChangeOrderType = table.Column<int>(nullable: false),
                    CustomerChangeLogEntryId = table.Column<int>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerHistoryEntries", x => x.CustomerHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_CustomerHistoryEntries_CustomerChangeLogEntries_CustomerChangeLogEntryId",
                        column: x => x.CustomerChangeLogEntryId,
                        principalTable: "CustomerChangeLogEntries",
                        principalColumn: "CustomerChangeLogEntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerHistoryEntries_CustomerChangeLogEntryId",
                table: "CustomerHistoryEntries",
                column: "CustomerChangeLogEntryId");
        }
    }
}
