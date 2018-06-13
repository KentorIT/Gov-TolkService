using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class InterpreterBrokers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterBroker_Brokers_BrokerId",
                table: "InterpreterBroker");

            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterBroker_Interpreters_InterpreterId",
                table: "InterpreterBroker");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterpreterBroker",
                table: "InterpreterBroker");

            migrationBuilder.RenameTable(
                name: "InterpreterBroker",
                newName: "InterpreterBrokers");

            migrationBuilder.RenameIndex(
                name: "IX_InterpreterBroker_InterpreterId",
                table: "InterpreterBrokers",
                newName: "IX_InterpreterBrokers_InterpreterId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterpreterBrokers",
                table: "InterpreterBrokers",
                columns: new[] { "BrokerId", "InterpreterId" });

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBrokers_Brokers_BrokerId",
                table: "InterpreterBrokers",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "BrokerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBrokers_Interpreters_InterpreterId",
                table: "InterpreterBrokers",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterBrokers_Brokers_BrokerId",
                table: "InterpreterBrokers");

            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterBrokers_Interpreters_InterpreterId",
                table: "InterpreterBrokers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterpreterBrokers",
                table: "InterpreterBrokers");

            migrationBuilder.RenameTable(
                name: "InterpreterBrokers",
                newName: "InterpreterBroker");

            migrationBuilder.RenameIndex(
                name: "IX_InterpreterBrokers_InterpreterId",
                table: "InterpreterBroker",
                newName: "IX_InterpreterBroker_InterpreterId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterpreterBroker",
                table: "InterpreterBroker",
                columns: new[] { "BrokerId", "InterpreterId" });

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBroker_Brokers_BrokerId",
                table: "InterpreterBroker",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "BrokerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBroker_Interpreters_InterpreterId",
                table: "InterpreterBroker",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
