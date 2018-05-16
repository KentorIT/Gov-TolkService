using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class Interpreter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InterpreterId",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Interpreter",
                columns: table => new
                {
                    InterpreterId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interpreter", x => x.InterpreterId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_InterpreterId",
                table: "AspNetUsers",
                column: "InterpreterId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Interpreter_InterpreterId",
                table: "AspNetUsers",
                column: "InterpreterId",
                principalTable: "Interpreter",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Interpreter_InterpreterId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Interpreter");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_InterpreterId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InterpreterId",
                table: "AspNetUsers");
        }
    }
}
