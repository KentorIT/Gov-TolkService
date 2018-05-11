using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddInterpreterAndmodifersOnRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrokerMessage",
                table: "Requests",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpersonatingModifier",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterpreterId",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedDate",
                table: "Requests",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ImpersonatingModifier",
                table: "Requests",
                column: "ImpersonatingModifier");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_InterpreterId",
                table: "Requests",
                column: "InterpreterId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ModifiedBy",
                table: "Requests",
                column: "ModifiedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingModifier",
                table: "Requests",
                column: "ImpersonatingModifier",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_AspNetUsers_InterpreterId",
                table: "Requests",
                column: "InterpreterId",
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ImpersonatingModifier",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_InterpreterId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_AspNetUsers_ModifiedBy",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ImpersonatingModifier",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_InterpreterId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ModifiedBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "BrokerMessage",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ImpersonatingModifier",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "InterpreterId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Requests");
        }
    }
}
