using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class InterpreterBrokerRegion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterpreterBrokerRegion",
                columns: table => new
                {
                    BrokerRegionId = table.Column<int>(nullable: false),
                    InterpreterId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterpreterBrokerRegion", x => new { x.BrokerRegionId, x.InterpreterId });
                    table.ForeignKey(
                        name: "FK_InterpreterBrokerRegion_BrokerRegions_BrokerRegionId",
                        column: x => x.BrokerRegionId,
                        principalTable: "BrokerRegions",
                        principalColumn: "BrokerRegionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterpreterBrokerRegion_AspNetUsers_InterpreterId",
                        column: x => x.InterpreterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterpreterBrokerRegion_InterpreterId",
                table: "InterpreterBrokerRegion",
                column: "InterpreterId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterpreterBrokerRegion");
        }
    }
}
