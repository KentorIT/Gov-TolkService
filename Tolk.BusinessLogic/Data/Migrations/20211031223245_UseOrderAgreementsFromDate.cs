using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class UseOrderAgreementsFromDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UseOrderAgreementsFromDate",
                table: "CustomerOrganisations",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UseOrderAgreementsFromDate",
                table: "CustomerOrganisationHistoryEntry",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseOrderAgreementsFromDate",
                table: "CustomerOrganisations");

            migrationBuilder.DropColumn(
                name: "UseOrderAgreementsFromDate",
                table: "CustomerOrganisationHistoryEntry");
        }
    }
}
