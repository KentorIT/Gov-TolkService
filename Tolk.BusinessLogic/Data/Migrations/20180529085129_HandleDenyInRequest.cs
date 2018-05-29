using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class HandleDenyInRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_AcceptanceBy",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingAcceptanceBy",
                table: "Requests");

            migrationBuilder.RenameColumn(
                name: "ImpersonatingAcceptanceBy",
                table: "Requests",
                newName: "ImpersonatingAnswerProcessedBy");

            migrationBuilder.RenameColumn(
                name: "AcceptanceDate",
                table: "Requests",
                newName: "AnswerProcessedDate");

            migrationBuilder.RenameColumn(
                name: "AcceptanceBy",
                table: "Requests",
                newName: "AnswerProcessedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_ImpersonatingAcceptanceBy",
                table: "Requests",
                newName: "IX_Requests_ImpersonatingAnswerProcessedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_AcceptanceBy",
                table: "Requests",
                newName: "IX_Requests_AnswerProcessedBy");

            migrationBuilder.AddColumn<string>(
                name: "DenyMessage",
                table: "Requests",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_AnswerProcessedBy",
                table: "Requests",
                column: "AnswerProcessedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingAnswerProcessedBy",
                table: "Requests",
                column: "ImpersonatingAnswerProcessedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_AnswerProcessedBy",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingAnswerProcessedBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "DenyMessage",
                table: "Requests");

            migrationBuilder.RenameColumn(
                name: "ImpersonatingAnswerProcessedBy",
                table: "Requests",
                newName: "ImpersonatingAcceptanceBy");

            migrationBuilder.RenameColumn(
                name: "AnswerProcessedDate",
                table: "Requests",
                newName: "AcceptanceDate");

            migrationBuilder.RenameColumn(
                name: "AnswerProcessedBy",
                table: "Requests",
                newName: "AcceptanceBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_ImpersonatingAnswerProcessedBy",
                table: "Requests",
                newName: "IX_Requests_ImpersonatingAcceptanceBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_AnswerProcessedBy",
                table: "Requests",
                newName: "IX_Requests_AcceptanceBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_AcceptanceBy",
                table: "Requests",
                column: "AcceptanceBy",
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
        }
    }
}
