using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class InterpreterBroker : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterpreterBrokerRegion");

            migrationBuilder.CreateTable(
                name: "InterpreterBroker",
                columns: table => new
                {
                    BrokerId = table.Column<int>(nullable: false),
                    InterpreterId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterpreterBroker", x => new { x.BrokerId, x.InterpreterId });
                    table.ForeignKey(
                        name: "FK_InterpreterBroker_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "BrokerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterpreterBroker_Interpreters_InterpreterId",
                        column: x => x.InterpreterId,
                        principalTable: "Interpreters",
                        principalColumn: "InterpreterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterpreterBroker_InterpreterId",
                table: "InterpreterBroker",
                column: "InterpreterId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterpreterBroker");

            migrationBuilder.CreateTable(
                name: "InterpreterBrokerRegion",
                columns: table => new
                {
                    BrokerId = table.Column<int>(nullable: false),
                    RegionId = table.Column<int>(nullable: false),
                    InterpreterId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterpreterBrokerRegion", x => new { x.BrokerId, x.RegionId, x.InterpreterId });
                    table.ForeignKey(
                        name: "FK_InterpreterBrokerRegion_Interpreters_InterpreterId",
                        column: x => x.InterpreterId,
                        principalTable: "Interpreters",
                        principalColumn: "InterpreterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterpreterBrokerRegion_BrokerRegions_BrokerId_RegionId",
                        columns: x => new { x.BrokerId, x.RegionId },
                        principalTable: "BrokerRegions",
                        principalColumns: new[] { "BrokerId", "RegionId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterpreterBrokerRegion_InterpreterId",
                table: "InterpreterBrokerRegion",
                column: "InterpreterId");
        }
    }
}
