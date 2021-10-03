using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderAgreementPayload_CreatorNotRequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderAgreementPayloads_AspNetUsers_CreatedBy",
                table: "OrderAgreementPayloads");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "OrderAgreementPayloads",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAgreementPayloads_AspNetUsers_CreatedBy",
                table: "OrderAgreementPayloads",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderAgreementPayloads_AspNetUsers_CreatedBy",
                table: "OrderAgreementPayloads");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "OrderAgreementPayloads",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAgreementPayloads_AspNetUsers_CreatedBy",
                table: "OrderAgreementPayloads",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
