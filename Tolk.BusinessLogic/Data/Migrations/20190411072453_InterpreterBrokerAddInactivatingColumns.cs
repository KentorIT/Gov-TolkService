using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class InterpreterBrokerAddInactivatingColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingInactivatedBy",
                table: "InterpreterBrokers",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InactivatedAt",
                table: "InterpreterBrokers",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "InactivatedBy",
                table: "InterpreterBrokers",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "InterpreterBrokers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_InterpreterBrokers_ImpersonatingInactivatedBy",
                table: "InterpreterBrokers",
                column: "ImpersonatingInactivatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InterpreterBrokers_InactivatedBy",
                table: "InterpreterBrokers",
                column: "InactivatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBrokers_AspNetUsers_ImpersonatingInactivatedBy",
                table: "InterpreterBrokers",
                column: "ImpersonatingInactivatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBrokers_AspNetUsers_InactivatedBy",
                table: "InterpreterBrokers",
                column: "InactivatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterBrokers_AspNetUsers_ImpersonatingInactivatedBy",
                table: "InterpreterBrokers");

            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterBrokers_AspNetUsers_InactivatedBy",
                table: "InterpreterBrokers");

            migrationBuilder.DropIndex(
                name: "IX_InterpreterBrokers_ImpersonatingInactivatedBy",
                table: "InterpreterBrokers");

            migrationBuilder.DropIndex(
                name: "IX_InterpreterBrokers_InactivatedBy",
                table: "InterpreterBrokers");

            migrationBuilder.DropColumn(
                name: "ImpersonatingInactivatedBy",
                table: "InterpreterBrokers");

            migrationBuilder.DropColumn(
                name: "InactivatedAt",
                table: "InterpreterBrokers");

            migrationBuilder.DropColumn(
                name: "InactivatedBy",
                table: "InterpreterBrokers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "InterpreterBrokers");
        }
    }
}
