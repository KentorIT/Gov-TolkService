using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderAgreementPayload_AddedcreatedByAndAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "OrderAgreementPayloads",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "OrderAgreementPayloads",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingCreatedBy",
                table: "OrderAgreementPayloads",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_CreatedBy",
                table: "OrderAgreementPayloads",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAgreementPayloads_AspNetUsers_CreatedBy",
                table: "OrderAgreementPayloads",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderAgreementPayloads_AspNetUsers_CreatedBy",
                table: "OrderAgreementPayloads");

            migrationBuilder.DropIndex(
                name: "IX_OrderAgreementPayloads_CreatedBy",
                table: "OrderAgreementPayloads");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "OrderAgreementPayloads");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "OrderAgreementPayloads");

            migrationBuilder.DropColumn(
                name: "ImpersonatingCreatedBy",
                table: "OrderAgreementPayloads");
        }
    }
}
