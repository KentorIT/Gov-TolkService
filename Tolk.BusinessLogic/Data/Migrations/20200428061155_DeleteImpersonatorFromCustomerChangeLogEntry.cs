using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class DeleteImpersonatorFromCustomerChangeLogEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerChangeLogEntries_AspNetUsers_UpdatedByImpersonatorId",
                table: "CustomerChangeLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_CustomerChangeLogEntries_UpdatedByImpersonatorId",
                table: "CustomerChangeLogEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedByImpersonatorId",
                table: "CustomerChangeLogEntries");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UpdatedByImpersonatorId",
                table: "CustomerChangeLogEntries",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerChangeLogEntries_UpdatedByImpersonatorId",
                table: "CustomerChangeLogEntries",
                column: "UpdatedByImpersonatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerChangeLogEntries_AspNetUsers_UpdatedByImpersonatorId",
                table: "CustomerChangeLogEntries",
                column: "UpdatedByImpersonatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
