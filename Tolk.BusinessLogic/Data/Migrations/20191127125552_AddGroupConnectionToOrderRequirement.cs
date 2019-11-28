using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddGroupConnectionToOrderRequirement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderGroupRequirementId",
                table: "OrderRequirements",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderRequirements_OrderGroupRequirementId",
                table: "OrderRequirements",
                column: "OrderGroupRequirementId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderRequirements_OrderGroupRequirements_OrderGroupRequirementId",
                table: "OrderRequirements",
                column: "OrderGroupRequirementId",
                principalTable: "OrderGroupRequirements",
                principalColumn: "OrderGroupRequirementId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderRequirements_OrderGroupRequirements_OrderGroupRequirementId",
                table: "OrderRequirements");

            migrationBuilder.DropIndex(
                name: "IX_OrderRequirements_OrderGroupRequirementId",
                table: "OrderRequirements");

            migrationBuilder.DropColumn(
                name: "OrderGroupRequirementId",
                table: "OrderRequirements");
        }
    }
}
