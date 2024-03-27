using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_CustomerOrderAgreementSettings_Per_Broker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerOrderAgreementSettings",
                columns: table => new
                {
                    CustomerOrganisationId = table.Column<int>(type: "int", nullable: false),
                    BrokerId = table.Column<int>(type: "int", nullable: false),
                    EnabledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrderAgreementSettings", x => new { x.BrokerId, x.CustomerOrganisationId });
                    table.ForeignKey(
                        name: "FK_CustomerOrderAgreementSettings_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "BrokerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrderAgreementSettings_CustomerOrganisations_CustomerOrganisationId",
                        column: x => x.CustomerOrganisationId,
                        principalTable: "CustomerOrganisations",
                        principalColumn: "CustomerOrganisationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerOrderAgreementSettingsHistoryEntries",
                columns: table => new
                {
                    CustomerOrderAgreementSettingsHistoryEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerChangeLogEntryId = table.Column<int>(type: "int", nullable: false),
                    BrokerId = table.Column<int>(type: "int", nullable: false),
                    EnabledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrderAgreementSettingsHistoryEntries", x => x.CustomerOrderAgreementSettingsHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_CustomerOrderAgreementSettingsHistoryEntries_CustomerChangeLogEntries_CustomerChangeLogEntryId",
                        column: x => x.CustomerChangeLogEntryId,
                        principalTable: "CustomerChangeLogEntries",
                        principalColumn: "CustomerChangeLogEntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderAgreementSettings_CustomerOrganisationId",
                table: "CustomerOrderAgreementSettings",
                column: "CustomerOrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderAgreementSettingsHistoryEntries_CustomerChangeLogEntryId",
                table: "CustomerOrderAgreementSettingsHistoryEntries",
                column: "CustomerChangeLogEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerOrderAgreementSettings");

            migrationBuilder.DropTable(
                name: "CustomerOrderAgreementSettingsHistoryEntries");
        }
    }
}
