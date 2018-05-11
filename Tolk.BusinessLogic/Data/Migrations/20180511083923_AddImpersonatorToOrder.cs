using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddImpersonatorToOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImpersonatingCreator",
                table: "Orders",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ImpersonatingCreator",
                table: "Orders",
                column: "ImpersonatingCreator");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_ImpersonatingCreator",
                table: "Orders",
                column: "ImpersonatingCreator",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_ImpersonatingCreator",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ImpersonatingCreator",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ImpersonatingCreator",
                table: "Orders");
        }
    }
}
