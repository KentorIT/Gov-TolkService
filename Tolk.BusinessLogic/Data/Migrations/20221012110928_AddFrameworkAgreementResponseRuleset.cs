using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddFrameworkAgreementResponseRuleset : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FrameworkAgreementResponseRuleset",
                table: "FrameworkAgreements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            //Set Ruleset on current FrameworkAgreement
            migrationBuilder.Sql("Update FrameworkAgreements Set FrameworkAgreementResponseRuleset = 1 Where FrameworkAgreementId = 1 ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FrameworkAgreementResponseRuleset",
                table: "FrameworkAgreements");
        }
    }
}
