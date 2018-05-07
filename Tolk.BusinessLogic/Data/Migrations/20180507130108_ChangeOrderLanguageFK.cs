using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ChangeOrderLanguageFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Languages_RegionId",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_LanguageId",
                table: "Orders",
                column: "LanguageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Languages_LanguageId",
                table: "Orders",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "LanguageId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Languages_LanguageId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_LanguageId",
                table: "Orders");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Languages_RegionId",
                table: "Orders",
                column: "RegionId",
                principalTable: "Languages",
                principalColumn: "LanguageId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
