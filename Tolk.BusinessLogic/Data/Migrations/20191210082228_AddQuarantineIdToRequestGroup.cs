using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddQuarantineIdToRequestGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuarantineId",
                table: "RequestGroups",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_QuarantineId",
                table: "RequestGroups",
                column: "QuarantineId");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestGroups_Quarantines_QuarantineId",
                table: "RequestGroups",
                column: "QuarantineId",
                principalTable: "Quarantines",
                principalColumn: "QuarantineId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestGroups_Quarantines_QuarantineId",
                table: "RequestGroups");

            migrationBuilder.DropIndex(
                name: "IX_RequestGroups_QuarantineId",
                table: "RequestGroups");

            migrationBuilder.DropColumn(
                name: "QuarantineId",
                table: "RequestGroups");
        }
    }
}
