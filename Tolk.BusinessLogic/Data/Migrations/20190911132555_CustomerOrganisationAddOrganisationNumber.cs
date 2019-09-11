using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class CustomerOrganisationAddOrganisationNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrganizationPrefix",
                table: "CustomerOrganisations",
                newName: "OrganisationPrefix");

            migrationBuilder.AddColumn<string>(
                name: "OrganisationNumber",
                table: "CustomerOrganisations",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganisationNumber",
                table: "CustomerOrganisations");

            migrationBuilder.RenameColumn(
                name: "OrganisationPrefix",
                table: "CustomerOrganisations",
                newName: "OrganizationPrefix");
        }
    }
}
