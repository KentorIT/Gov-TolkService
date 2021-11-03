using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class customerChangeHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerOrganisationHistoryEntry",
                columns: table => new
                {
                    CustomerOrganisationHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    PriceListType = table.Column<int>(nullable: false),
                    EmailDomain = table.Column<string>(maxLength: 50, nullable: true),
                    OrganisationPrefix = table.Column<string>(maxLength: 8, nullable: true),
                    OrganisationNumber = table.Column<string>(maxLength: 32, nullable: false),
                    PeppolId = table.Column<string>(maxLength: 50, nullable: true),
                    TravelCostAgreementType = table.Column<int>(nullable: false),
                    CustomerChangeLogEntryId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrganisationHistoryEntry", x => x.CustomerOrganisationHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_CustomerOrganisationHistoryEntry_CustomerChangeLogEntries_CustomerChangeLogEntryId",
                        column: x => x.CustomerChangeLogEntryId,
                        principalTable: "CustomerChangeLogEntries",
                        principalColumn: "CustomerChangeLogEntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrganisationHistoryEntry_CustomerChangeLogEntryId",
                table: "CustomerOrganisationHistoryEntry",
                column: "CustomerChangeLogEntryId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerOrganisationHistoryEntry");
        }
    }
}
