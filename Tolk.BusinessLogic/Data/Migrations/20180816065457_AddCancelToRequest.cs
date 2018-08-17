using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddCancelToRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelMessage",
                table: "Requests",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledBy",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingCanceller",
                table: "Requests",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_CancelledBy",
                table: "Requests",
                column: "CancelledBy");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ImpersonatingCanceller",
                table: "Requests",
                column: "ImpersonatingCanceller");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_CancelledBy",
                table: "Requests",
                column: "CancelledBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingCanceller",
                table: "Requests",
                column: "ImpersonatingCanceller",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_CancelledBy",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingCanceller",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_CancelledBy",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ImpersonatingCanceller",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "CancelMessage",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ImpersonatingCanceller",
                table: "Requests");
        }
    }
}
