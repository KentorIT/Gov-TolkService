using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIdToHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerOrganisationId",
                table: "AspNetUserHistoryEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserHistoryEntries_CustomerOrganisations_CustomerOrganisationId",
                table: "AspNetUserHistoryEntries",
                column: "CustomerOrganisationId",
                principalTable: "CustomerOrganisations",
                principalColumn: "CustomerOrganisationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserHistoryEntries_CustomerOrganisations_CustomerOrganisationId",
                table: "AspNetUserHistoryEntries");

            migrationBuilder.DropColumn(
                name: "CustomerOrganisationId",
                table: "AspNetUserHistoryEntries");
        }
    }
}
