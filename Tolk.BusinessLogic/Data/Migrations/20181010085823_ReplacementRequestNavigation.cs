using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ReplacementRequestNavigation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReplacingRequestId",
                table: "Requests",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ReplacingRequestId",
                table: "Requests",
                column: "ReplacingRequestId",
                unique: true,
                filter: "[ReplacingRequestId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Requests_ReplacingRequestId",
                table: "Requests",
                column: "ReplacingRequestId",
                principalTable: "Requests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Requests_ReplacingRequestId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ReplacingRequestId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ReplacingRequestId",
                table: "Requests");
        }
    }
}
