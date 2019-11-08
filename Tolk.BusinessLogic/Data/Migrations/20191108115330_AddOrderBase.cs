using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddOrderBase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AssignentType",
                newName: "AssignmentType",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "AllowExceedingTravelCost",
                table: "OrderGroups",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignmentType",
                table: "OrderGroups",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "CustomerOrganisationId",
                table: "OrderGroups",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "CustomerUnitId",
                table: "OrderGroups",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LanguageHasAuthorizedInterpreter",
                table: "OrderGroups",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "LanguageId",
                table: "OrderGroups",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherLanguage",
                table: "OrderGroups",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegionId",
                table: "OrderGroups",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "SpecificCompetenceLevelRequired",
                table: "OrderGroups",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OrderGroups",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroups_CustomerOrganisationId",
                table: "OrderGroups",
                column: "CustomerOrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroups_CustomerUnitId",
                table: "OrderGroups",
                column: "CustomerUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroups_LanguageId",
                table: "OrderGroups",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroups_RegionId",
                table: "OrderGroups",
                column: "RegionId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderGroups_CustomerOrganisations_CustomerOrganisationId",
                table: "OrderGroups",
                column: "CustomerOrganisationId",
                principalTable: "CustomerOrganisations",
                principalColumn: "CustomerOrganisationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderGroups_CustomerUnits_CustomerUnitId",
                table: "OrderGroups",
                column: "CustomerUnitId",
                principalTable: "CustomerUnits",
                principalColumn: "CustomerUnitId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderGroups_Languages_LanguageId",
                table: "OrderGroups",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "LanguageId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderGroups_Regions_RegionId",
                table: "OrderGroups",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "RegionId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderGroups_CustomerOrganisations_CustomerOrganisationId",
                table: "OrderGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderGroups_CustomerUnits_CustomerUnitId",
                table: "OrderGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderGroups_Languages_LanguageId",
                table: "OrderGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderGroups_Regions_RegionId",
                table: "OrderGroups");

            migrationBuilder.DropIndex(
                name: "IX_OrderGroups_CustomerOrganisationId",
                table: "OrderGroups");

            migrationBuilder.DropIndex(
                name: "IX_OrderGroups_CustomerUnitId",
                table: "OrderGroups");

            migrationBuilder.DropIndex(
                name: "IX_OrderGroups_LanguageId",
                table: "OrderGroups");

            migrationBuilder.DropIndex(
                name: "IX_OrderGroups_RegionId",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "AllowExceedingTravelCost",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "AssignmentType",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "CustomerOrganisationId",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "CustomerUnitId",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "LanguageHasAuthorizedInterpreter",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "OtherLanguage",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "RegionId",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "SpecificCompetenceLevelRequired",
                table: "OrderGroups");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrderGroups");

            migrationBuilder.RenameColumn(
                name: "AssignmentType",
                newName: "AssignentType",
                table: "Orders");
        }
    }
}
