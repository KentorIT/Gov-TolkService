using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RemoveDuplicateRequestRankingId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Rankings_RankingId1",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_RankingId1",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RankingId1",
                table: "Requests");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RankingId1",
                table: "Requests",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RankingId1",
                table: "Requests",
                column: "RankingId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Rankings_RankingId1",
                table: "Requests",
                column: "RankingId1",
                principalTable: "Rankings",
                principalColumn: "RankingId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
