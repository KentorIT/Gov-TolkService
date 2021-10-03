using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderAgreementPayload_FKToImpersonator : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_ImpersonatingCreatedBy",
                table: "OrderAgreementPayloads",
                column: "ImpersonatingCreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAgreementPayloads_AspNetUsers_ImpersonatingCreatedBy",
                table: "OrderAgreementPayloads",
                column: "ImpersonatingCreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderAgreementPayloads_AspNetUsers_ImpersonatingCreatedBy",
                table: "OrderAgreementPayloads");

            migrationBuilder.DropIndex(
                name: "IX_OrderAgreementPayloads_ImpersonatingCreatedBy",
                table: "OrderAgreementPayloads");
        }
    }
}
