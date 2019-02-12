using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddColumnSystemMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingLastUpdated",
                table: "SystemMessages",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemMessages_ImpersonatingLastUpdated",
                table: "SystemMessages",
                column: "ImpersonatingLastUpdated");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemMessages_AspNetUsers_ImpersonatingLastUpdated",
                table: "SystemMessages",
                column: "ImpersonatingLastUpdated",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SystemMessages_AspNetUsers_ImpersonatingLastUpdated",
                table: "SystemMessages");

            migrationBuilder.DropIndex(
                name: "IX_SystemMessages_ImpersonatingLastUpdated",
                table: "SystemMessages");

            migrationBuilder.DropColumn(
                name: "ImpersonatingLastUpdated",
                table: "SystemMessages");
        }
    }
}
