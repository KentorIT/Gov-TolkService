using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class NullableTaxCard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "InterpretersTaxCard",
                table: "Requisitions",
                nullable: true,
                oldClrType: typeof(int));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "InterpretersTaxCard",
                table: "Requisitions",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);
        }
    }
}
