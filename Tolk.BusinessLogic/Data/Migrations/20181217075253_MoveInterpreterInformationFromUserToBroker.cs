using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class MoveInterpreterInformationFromUserToBroker : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterBrokers_Interpreters_InterpreterId",
                table: "InterpreterBrokers");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Interpreters_InterpreterId",
                table: "Requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterpreterBrokers",
                table: "InterpreterBrokers");

            migrationBuilder.DropColumn(
                name: "AcceptedByInterpreter",
                table: "InterpreterBrokers");

            migrationBuilder.RenameColumn(
                name: "InterpreterId",
                table: "Requests",
                newName: "InterpreterBrokerId");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_InterpreterId",
                table: "Requests",
                newName: "IX_Requests_InterpreterBrokerId");

            migrationBuilder.AlterColumn<int>(
                name: "InterpreterId",
                table: "InterpreterBrokers",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<int>(
                name: "InterpreterBrokerId",
                table: "InterpreterBrokers",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "InterpreterBrokers",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "InterpreterBrokers",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "InterpreterBrokers",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OfficialInterpreterId",
                table: "InterpreterBrokers",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "InterpreterBrokers",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterpreterBrokers",
                table: "InterpreterBrokers",
                column: "InterpreterBrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_InterpreterBrokers_BrokerId",
                table: "InterpreterBrokers",
                column: "BrokerId");

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBrokers_Interpreters_InterpreterId",
                table: "InterpreterBrokers",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_InterpreterBrokers_InterpreterBrokerId",
                table: "Requests",
                column: "InterpreterBrokerId",
                principalTable: "InterpreterBrokers",
                principalColumn: "InterpreterBrokerId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterBrokers_Interpreters_InterpreterId",
                table: "InterpreterBrokers");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_InterpreterBrokers_InterpreterBrokerId",
                table: "Requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterpreterBrokers",
                table: "InterpreterBrokers");

            migrationBuilder.DropIndex(
                name: "IX_InterpreterBrokers_BrokerId",
                table: "InterpreterBrokers");

            migrationBuilder.DropColumn(
                name: "InterpreterBrokerId",
                table: "InterpreterBrokers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "InterpreterBrokers");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "InterpreterBrokers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "InterpreterBrokers");

            migrationBuilder.DropColumn(
                name: "OfficialInterpreterId",
                table: "InterpreterBrokers");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "InterpreterBrokers");

            migrationBuilder.RenameColumn(
                name: "InterpreterBrokerId",
                table: "Requests",
                newName: "InterpreterId");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_InterpreterBrokerId",
                table: "Requests",
                newName: "IX_Requests_InterpreterId");

            migrationBuilder.AlterColumn<int>(
                name: "InterpreterId",
                table: "InterpreterBrokers",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptedByInterpreter",
                table: "InterpreterBrokers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterpreterBrokers",
                table: "InterpreterBrokers",
                columns: new[] { "BrokerId", "InterpreterId" });

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBrokers_Interpreters_InterpreterId",
                table: "InterpreterBrokers",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Interpreters_InterpreterId",
                table: "Requests",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
