using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddColumnInterpreterCompetenceVerificationResultOnStart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InterpreterCompetenceVerificationResultOnStart",
                table: "Requests",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterpreterCompetenceVerificationResultOnStart",
                table: "Requests");
        }
    }
}
