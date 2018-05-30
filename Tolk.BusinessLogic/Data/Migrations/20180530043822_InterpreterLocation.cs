using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class InterpreterLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedInterpreterLocation",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RequiredInterpreterLocation",
                table: "Orders");

            migrationBuilder.CreateTable(
                name: "OrderInterpreterLocation",
                columns: table => new
                {
                    InterpreterLocation = table.Column<int>(nullable: false),
                    Rank = table.Column<int>(nullable: false),
                    OrderId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderInterpreterLocation", x => new { x.OrderId, x.InterpreterLocation });
                    table.ForeignKey(
                        name: "FK_OrderInterpreterLocation_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderInterpreterLocation");

            migrationBuilder.AddColumn<int>(
                name: "RequestedInterpreterLocation",
                table: "Orders",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredInterpreterLocation",
                table: "Orders",
                nullable: false,
                defaultValue: 0);
        }
    }
}
