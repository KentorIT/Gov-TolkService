using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddTablesCustomerSettingsAndChangeHistories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerChangeLogEntries",
                columns: table => new
                {
                    CustomerChangeLogEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CustomerChangeLogType = table.Column<int>(nullable: false),
                    UpdatedByUserId = table.Column<int>(nullable: false),
                    UpdatedByImpersonatorId = table.Column<int>(nullable: true),
                    LoggedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerChangeLogEntries", x => x.CustomerChangeLogEntryId);
                    table.ForeignKey(
                        name: "FK_CustomerChangeLogEntries_AspNetUsers_UpdatedByImpersonatorId",
                        column: x => x.UpdatedByImpersonatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerChangeLogEntries_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSettings",
                columns: table => new
                {
                    CustomerOrganisationId = table.Column<int>(nullable: false),
                    CustomerSettingType = table.Column<int>(nullable: false),
                    Value = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSettings", x => new { x.CustomerOrganisationId, x.CustomerSettingType });
                    table.ForeignKey(
                        name: "FK_CustomerSettings_CustomerOrganisations_CustomerOrganisationId",
                        column: x => x.CustomerOrganisationId,
                        principalTable: "CustomerOrganisations",
                        principalColumn: "CustomerOrganisationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerHistoryEntries",
                columns: table => new
                {
                    CustomerHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CustomerChangeLogEntryId = table.Column<int>(nullable: false),
                    ChangeOrderType = table.Column<int>(nullable: false),
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
                name: "IX_CustomerChangeLogEntries_UpdatedByImpersonatorId",
                table: "CustomerChangeLogEntries",
                column: "UpdatedByImpersonatorId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerChangeLogEntries_UpdatedByUserId",
                table: "CustomerChangeLogEntries",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerHistoryEntries_CustomerChangeLogEntryId",
                table: "CustomerHistoryEntries",
                column: "CustomerChangeLogEntryId");


            migrationBuilder.Sql(@"
            INSERT INTO CustomerSettings(CustomerOrganisationId, CustomerSettingType, Value)
            SELECT CustomerOrganisationId, 1, UseOrderGroups FROM CustomerOrganisations
            
            INSERT INTO CustomerSettings(CustomerOrganisationId, CustomerSettingType, Value)
            SELECT CustomerOrganisationId, 2, UseSelfInvoicingInterpreter FROM CustomerOrganisations

            --all customers should have possibilty to use attachments from the beginning
            INSERT INTO CustomerSettings (CustomerOrganisationId, CustomerSettingType, Value)
            SELECT CustomerOrganisationId, 3, 1  FROM CustomerOrganisations"
            );

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerHistoryEntries");

            migrationBuilder.DropTable(
                name: "CustomerSettings");

            migrationBuilder.DropTable(
                name: "CustomerChangeLogEntries");
        }
    }
}
