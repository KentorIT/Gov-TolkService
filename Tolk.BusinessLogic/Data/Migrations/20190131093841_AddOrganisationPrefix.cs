using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddOrganisationPrefix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizationPrefix",
                table: "CustomerOrganisations",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmailDomain",
                table: "Brokers",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationPrefix",
                table: "Brokers",
                maxLength: 8,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationPrefix",
                table: "CustomerOrganisations");

            migrationBuilder.DropColumn(
                name: "OrganizationPrefix",
                table: "Brokers");

            migrationBuilder.AlterColumn<string>(
                name: "EmailDomain",
                table: "Brokers",
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
