using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RankingDatesNonAmbigousNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn("StartDate", "Rankings", "FirstValidDate");
            migrationBuilder.AlterColumn<DateTime>(
                "FirstValidDate",
                "Rankings",
                "date");

            migrationBuilder.RenameColumn("EndDate", "Rankings", "LastValidDate");
            migrationBuilder.AlterColumn<DateTime>(
                "LastValidDate",
                "Rankings",
                "date");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                "FirstValidDate",
                "Rankings",
                oldType: "date");

            migrationBuilder.RenameColumn("FirstValidDate", "Rankings", "StartDate");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                "LastValidDate",
                "Rankings",
                oldType: "date");

            migrationBuilder.RenameColumn("LastValidDate", "Rankings", "EndDate");
        }
    }
}
