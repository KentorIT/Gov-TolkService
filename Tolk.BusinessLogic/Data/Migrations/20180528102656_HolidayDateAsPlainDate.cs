using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class HolidayDateAsPlainDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey("PK_Holidays", "Holidays");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Holidays",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime));

            migrationBuilder.AddPrimaryKey("PK_Holidays", "Holidays", "Date");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey("PK_Holidays", "Holidays");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Holidays",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AddPrimaryKey("PK_Holidays", "Holidays", "Date");
        }
    }
}
