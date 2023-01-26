using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class Alter_FrameworkAgreement_Remove_PossibleExtensionInMonths : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PossibleAgreementExtensionsInMonths",
                table: "FrameworkAgreements");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PossibleAgreementExtensionsInMonths",
                table: "FrameworkAgreements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
