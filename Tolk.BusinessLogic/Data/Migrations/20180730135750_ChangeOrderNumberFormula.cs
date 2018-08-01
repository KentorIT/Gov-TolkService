using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ChangeOrderNumberFormula : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OrderNumber",
                table: "Orders",
                nullable: false,
                computedColumnSql: "CAST(YEAR([CreatedAt]) AS NVARCHAR(MAX)) + '-' + CAST(([OrderId]+(100000)) AS NVARCHAR(MAX))",
                oldClrType: typeof(int),
                oldComputedColumnSql: "[OrderId] + 10000000");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OrderNumber",
                table: "Orders",
                nullable: false,
                computedColumnSql: "[OrderId] + 10000000",
                oldClrType: typeof(int),
                oldComputedColumnSql: "CAST(YEAR([CreatedAt]) AS NVARCHAR(MAX)) + '-' + CAST(([OrderId]+(100000)) AS NVARCHAR(MAX))");
        }
    }
}
