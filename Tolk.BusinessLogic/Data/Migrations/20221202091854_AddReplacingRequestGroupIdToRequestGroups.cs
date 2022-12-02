using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddReplacingRequestGroupIdToRequestGroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReplacingRequestGroupId",
                table: "RequestGroups",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_ReplacingRequestGroupId",
                table: "RequestGroups",
                column: "ReplacingRequestGroupId",
                unique: true,
                filter: "[ReplacingRequestGroupId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestGroups_RequestGroups_ReplacingRequestGroupId",
                table: "RequestGroups",
                column: "ReplacingRequestGroupId",
                principalTable: "RequestGroups",
                principalColumn: "RequestGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestGroups_RequestGroups_ReplacingRequestGroupId",
                table: "RequestGroups");

            migrationBuilder.DropIndex(
                name: "IX_RequestGroups_ReplacingRequestGroupId",
                table: "RequestGroups");

            migrationBuilder.DropColumn(
                name: "ReplacingRequestGroupId",
                table: "RequestGroups");
        }
    }
}
