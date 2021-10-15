using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderAgreementPayloadAddIdentificationNumberColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentificationNumber",
                table: "OrderAgreementPayloads",
                maxLength: 32,
                nullable: false,
                defaultValue: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentificationNumber",
                table: "OrderAgreementPayloads");
        }
    }
}
