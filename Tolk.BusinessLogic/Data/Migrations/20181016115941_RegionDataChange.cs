using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RegionDataChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 21,
                column: "Name",
                value: "Jämtland");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 21,
                column: "Name",
                value: "Jämtland Härjedalen");
        }
    }
}
