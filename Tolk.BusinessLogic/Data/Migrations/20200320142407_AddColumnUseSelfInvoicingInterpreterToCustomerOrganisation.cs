using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddColumnUseSelfInvoicingInterpreterToCustomerOrganisation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseSelfInvoicingInterpreter",
                table: "CustomerOrganisations",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseSelfInvoicingInterpreter",
                table: "CustomerOrganisations");
        }
    }
}
