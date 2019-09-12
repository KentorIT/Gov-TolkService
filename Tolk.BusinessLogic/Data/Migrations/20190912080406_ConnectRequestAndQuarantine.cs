using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ConnectRequestAndQuarantine : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuarantineId",
                table: "Requests",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_QuarantineId",
                table: "Requests",
                column: "QuarantineId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Quarantines_QuarantineId",
                table: "Requests",
                column: "QuarantineId",
                principalTable: "Quarantines",
                principalColumn: "QuarantineId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Quarantines_QuarantineId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_QuarantineId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "QuarantineId",
                table: "Requests");
        }
    }
}
