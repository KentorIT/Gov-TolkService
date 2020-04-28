using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class DropColumnsFromOrderAddColumnToCustomerChangeLogEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseOrderGroups",
                table: "CustomerOrganisations");

            migrationBuilder.DropColumn(
                name: "UseSelfInvoicingInterpreter",
                table: "CustomerOrganisations");

            migrationBuilder.AddColumn<int>(
                name: "CustomerOrganisationId",
                table: "CustomerChangeLogEntries",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerChangeLogEntries_CustomerOrganisationId",
                table: "CustomerChangeLogEntries",
                column: "CustomerOrganisationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerChangeLogEntries_CustomerOrganisations_CustomerOrganisationId",
                table: "CustomerChangeLogEntries",
                column: "CustomerOrganisationId",
                principalTable: "CustomerOrganisations",
                principalColumn: "CustomerOrganisationId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerChangeLogEntries_CustomerOrganisations_CustomerOrganisationId",
                table: "CustomerChangeLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_CustomerChangeLogEntries_CustomerOrganisationId",
                table: "CustomerChangeLogEntries");

            migrationBuilder.DropColumn(
                name: "CustomerOrganisationId",
                table: "CustomerChangeLogEntries");

            migrationBuilder.AddColumn<bool>(
                name: "UseOrderGroups",
                table: "CustomerOrganisations",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseSelfInvoicingInterpreter",
                table: "CustomerOrganisations",
                nullable: false,
                defaultValue: false);
        }
    }
}
