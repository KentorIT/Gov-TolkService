using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddAcceptanceColumnsForRequestBase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcceptedBy",
                table: "Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingAcceptedBy",
                table: "Requests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAcceptAt",
                table: "Requests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestAnswerRuleType",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "AcceptedBy",
                table: "RequestGroups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingAcceptedBy",
                table: "RequestGroups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastAcceptAt",
                table: "RequestGroups",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestAnswerRuleType",
                table: "RequestGroups",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_AcceptedBy",
                table: "Requests",
                column: "AcceptedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ImpersonatingAcceptedBy",
                table: "Requests",
                column: "ImpersonatingAcceptedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_AcceptedBy",
                table: "RequestGroups",
                column: "AcceptedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_ImpersonatingAcceptedBy",
                table: "RequestGroups",
                column: "ImpersonatingAcceptedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestGroups_AspNetUsers_AcceptedBy",
                table: "RequestGroups",
                column: "AcceptedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestGroups_AspNetUsers_ImpersonatingAcceptedBy",
                table: "RequestGroups",
                column: "ImpersonatingAcceptedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_AcceptedBy",
                table: "Requests",
                column: "AcceptedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingAcceptedBy",
                table: "Requests",
                column: "ImpersonatingAcceptedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestGroups_AspNetUsers_AcceptedBy",
                table: "RequestGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestGroups_AspNetUsers_ImpersonatingAcceptedBy",
                table: "RequestGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_AcceptedBy",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingAcceptedBy",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_AcceptedBy",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ImpersonatingAcceptedBy",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_RequestGroups_AcceptedBy",
                table: "RequestGroups");

            migrationBuilder.DropIndex(
                name: "IX_RequestGroups_ImpersonatingAcceptedBy",
                table: "RequestGroups");

            migrationBuilder.DropColumn(
                name: "AcceptedBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ImpersonatingAcceptedBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "LastAcceptAt",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RequestAnswerRuleType",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "AcceptedBy",
                table: "RequestGroups");

            migrationBuilder.DropColumn(
                name: "ImpersonatingAcceptedBy",
                table: "RequestGroups");

            migrationBuilder.DropColumn(
                name: "LastAcceptAt",
                table: "RequestGroups");

            migrationBuilder.DropColumn(
                name: "RequestAnswerRuleType",
                table: "RequestGroups");
        }
    }
}
