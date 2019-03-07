using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ChangedColNameToInterpreterCompetenceVerificationResultOnAssign : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InterpreterCompetenceVerificationResult",
                newName: "InterpreterCompetenceVerificationResultOnAssign",
                table: "Requests");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InterpreterCompetenceVerificationResultOnAssign",
                newName: "InterpreterCompetenceVerificationResult",
                table: "Requests");
        }
    }
}
