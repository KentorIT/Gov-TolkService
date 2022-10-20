using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class LanguageNotRequired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Languages_LanguageId",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "LanguageId",
                table: "Orders",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.DropColumn(
                name:"OrderNumber",
                table:"Orders"
                );

            migrationBuilder.AddColumn<string>(
                 name: "OrderNumber",
                 table: "Orders",
                 nullable: false,
                 computedColumnSql: "CAST(YEAR([CreatedAt]) AS NVARCHAR(MAX)) + '-' + CAST(([OrderId]+(100000)) AS NVARCHAR(MAX))"
                );

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Languages_LanguageId",
                table: "Orders",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "LanguageId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Languages_LanguageId",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "LanguageId",
                table: "Orders",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.DropColumn(
               name: "OrderNumber",
               table: "Orders"
               );

            migrationBuilder.AddColumn<int>(
               name: "OrderNumber",
               table: "Orders",
               nullable: false,
               computedColumnSql: "CAST(YEAR([CreatedAt]) AS NVARCHAR(MAX)) + '-' + CAST(([OrderId]+(100000)) AS NVARCHAR(MAX))"
              );            

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Languages_LanguageId",
                table: "Orders",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "LanguageId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
