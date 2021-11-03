using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RequestAnwerProcessedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AnswerProcessedDate",
                table: "Requests",
                newName: "AnswerProcessedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AnswerProcessedAt",
                table: "Requests",
                newName: "AnswerProcessedDate");
        }
    }
}
