using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RequestInterpreterFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_InterpreterId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_InterpreterId",
                table: "Requests");

            migrationBuilder.AlterColumn<int>(
                name: "InterpreterId",
                table: "Requests",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_InterpreterId",
                table: "Requests",
                column: "InterpreterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Interpreters_InterpreterId",
                table: "Requests",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Interpreters_InterpreterId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_InterpreterId",
                table: "Requests");

            migrationBuilder.AlterColumn<string>(
                name: "InterpreterId",
                table: "Requests",
                nullable: true,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_InterpreterId",
                table: "Requests",
                column: "InterpreterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_InterpreterId",
                table: "Requests",
                column: "InterpreterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
