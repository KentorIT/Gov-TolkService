using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RegionIdrename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Regions",
                newName: "RegionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RegionId",
                table: "Regions",
                newName: "Id");
        }
    }
}
