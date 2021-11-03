using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddUserConnectionsToRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingModifier",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ModifiedBy",
                table: "Requests");

            migrationBuilder.RenameColumn(
                name: "ModifiedDate",
                table: "Requests",
                newName: "RecieveDate");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "Requests",
                newName: "ReceivedBy");

            migrationBuilder.RenameColumn(
                name: "ImpersonatingModifier",
                table: "Requests",
                newName: "ImpersonatingReceivedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_ModifiedBy",
                table: "Requests",
                newName: "IX_Requests_ReceivedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_ImpersonatingModifier",
                table: "Requests",
                newName: "IX_Requests_ImpersonatingReceivedBy");

            migrationBuilder.AddColumn<int>(
                name: "AcceptanceBy",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptanceDate",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AnswerDate",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnsweredBy",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingAcceptanceBy",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingAnsweredBy",
                table: "Requests",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_AcceptanceBy",
                table: "Requests",
                column: "AcceptanceBy");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_AnsweredBy",
                table: "Requests",
                column: "AnsweredBy");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ImpersonatingAcceptanceBy",
                table: "Requests",
                column: "ImpersonatingAcceptanceBy");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ImpersonatingAnsweredBy",
                table: "Requests",
                column: "ImpersonatingAnsweredBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_AcceptanceBy",
                table: "Requests",
                column: "AcceptanceBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_AnsweredBy",
                table: "Requests",
                column: "AnsweredBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingAcceptanceBy",
                table: "Requests",
                column: "ImpersonatingAcceptanceBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingAnsweredBy",
                table: "Requests",
                column: "ImpersonatingAnsweredBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingReceivedBy",
                table: "Requests",
                column: "ImpersonatingReceivedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_ReceivedBy",
                table: "Requests",
                column: "ReceivedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_AcceptanceBy",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_AnsweredBy",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingAcceptanceBy",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingAnsweredBy",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingReceivedBy",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ReceivedBy",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_AcceptanceBy",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_AnsweredBy",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ImpersonatingAcceptanceBy",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ImpersonatingAnsweredBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "AcceptanceBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "AcceptanceDate",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "AnswerDate",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "AnsweredBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ImpersonatingAcceptanceBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ImpersonatingAnsweredBy",
                table: "Requests");

            migrationBuilder.RenameColumn(
                name: "RecieveDate",
                table: "Requests",
                newName: "ModifiedDate");

            migrationBuilder.RenameColumn(
                name: "ReceivedBy",
                table: "Requests",
                newName: "ModifiedBy");

            migrationBuilder.RenameColumn(
                name: "ImpersonatingReceivedBy",
                table: "Requests",
                newName: "ImpersonatingModifier");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_ReceivedBy",
                table: "Requests",
                newName: "IX_Requests_ModifiedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_ImpersonatingReceivedBy",
                table: "Requests",
                newName: "IX_Requests_ImpersonatingModifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingModifier",
                table: "Requests",
                column: "ImpersonatingModifier",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_ModifiedBy",
                table: "Requests",
                column: "ModifiedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
