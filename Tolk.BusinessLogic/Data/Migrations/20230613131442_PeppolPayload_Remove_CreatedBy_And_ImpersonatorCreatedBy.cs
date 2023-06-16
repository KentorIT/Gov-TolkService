using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class PeppolPayload_Remove_CreatedBy_And_ImpersonatorCreatedBy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PeppolPayloads_AspNetUsers_CreatedBy",
                table: "PeppolPayloads");

            migrationBuilder.DropForeignKey(
                name: "FK_PeppolPayloads_AspNetUsers_ImpersonatingCreatedBy",
                table: "PeppolPayloads");

            migrationBuilder.DropIndex(
                name: "IX_PeppolPayloads_CreatedBy",
                table: "PeppolPayloads");

            migrationBuilder.DropIndex(
                name: "IX_PeppolPayloads_ImpersonatingCreatedBy",
                table: "PeppolPayloads");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PeppolPayloads");

            migrationBuilder.DropColumn(
                name: "ImpersonatingCreatedBy",
                table: "PeppolPayloads");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "PeppolPayloads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingCreatedBy",
                table: "PeppolPayloads",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_CreatedBy",
                table: "PeppolPayloads",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PeppolPayloads_ImpersonatingCreatedBy",
                table: "PeppolPayloads",
                column: "ImpersonatingCreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_PeppolPayloads_AspNetUsers_CreatedBy",
                table: "PeppolPayloads",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PeppolPayloads_AspNetUsers_ImpersonatingCreatedBy",
                table: "PeppolPayloads",
                column: "ImpersonatingCreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
