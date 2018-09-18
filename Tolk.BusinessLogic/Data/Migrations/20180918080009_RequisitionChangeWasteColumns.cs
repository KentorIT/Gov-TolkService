using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RequisitionChangeWasteColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeWasteAfterEndedAt",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "TimeWasteBeforeStartedAt",
                table: "Requisitions");

            migrationBuilder.AddColumn<int>(
                name: "TimeWasteIWHTime",
                table: "Requisitions",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeWasteNormalTime",
                table: "Requisitions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeWasteIWHTime",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "TimeWasteNormalTime",
                table: "Requisitions");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeWasteAfterEndedAt",
                table: "Requisitions",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TimeWasteBeforeStartedAt",
                table: "Requisitions",
                nullable: true);
        }
    }
}
