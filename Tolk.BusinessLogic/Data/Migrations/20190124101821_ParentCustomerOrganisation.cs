using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ParentCustomerOrganisation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CustomerOrganisations",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 20);

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "CustomerOrganisations",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParentCustomerOrganisationId",
                table: "CustomerOrganisations",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrganisations_ParentCustomerOrganisationId",
                table: "CustomerOrganisations",
                column: "ParentCustomerOrganisationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerOrganisations_CustomerOrganisations_ParentCustomerOrganisationId",
                table: "CustomerOrganisations",
                column: "ParentCustomerOrganisationId",
                principalTable: "CustomerOrganisations",
                principalColumn: "CustomerOrganisationId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerOrganisations_CustomerOrganisations_ParentCustomerOrganisationId",
                table: "CustomerOrganisations");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrganisations_ParentCustomerOrganisationId",
                table: "CustomerOrganisations");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "CustomerOrganisations");

            migrationBuilder.DropColumn(
                name: "ParentCustomerOrganisationId",
                table: "CustomerOrganisations");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CustomerOrganisations",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 100);
        }
    }
}
