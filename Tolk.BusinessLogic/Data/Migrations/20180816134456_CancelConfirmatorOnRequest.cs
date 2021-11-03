using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class CancelConfirmatorOnRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelConfirmedAt",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelConfirmedBy",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingCancelConfirmer",
                table: "Requests",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CancelConfirmedBy",
                table: "Requests",
                column: "CancelConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ImpersonatingCancelConfirmer",
                table: "Requests",
                column: "ImpersonatingCancelConfirmer");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_CancelConfirmedBy",
                table: "Requests",
                column: "CancelConfirmedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingCancelConfirmer",
                table: "Requests",
                column: "ImpersonatingCancelConfirmer",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_CancelConfirmedBy",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingCancelConfirmer",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_CancelConfirmedBy",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ImpersonatingCancelConfirmer",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "CancelConfirmedAt",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "CancelConfirmedBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ImpersonatingCancelConfirmer",
                table: "Requests");
        }
    }
}
