using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class DropBrokerRegion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rankings_BrokerRegions_BrokerId_RegionId",
                table: "Rankings");

            migrationBuilder.DropTable(
                name: "BrokerRegions");

            migrationBuilder.DropIndex(
                name: "IX_Rankings_BrokerId_RegionId",
                table: "Rankings");

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_BrokerId",
                table: "Rankings",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_RegionId",
                table: "Rankings",
                column: "RegionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rankings_Brokers_BrokerId",
                table: "Rankings",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "BrokerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rankings_Regions_RegionId",
                table: "Rankings",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "RegionId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rankings_Brokers_BrokerId",
                table: "Rankings");

            migrationBuilder.DropForeignKey(
                name: "FK_Rankings_Regions_RegionId",
                table: "Rankings");

            migrationBuilder.DropIndex(
                name: "IX_Rankings_BrokerId",
                table: "Rankings");

            migrationBuilder.DropIndex(
                name: "IX_Rankings_RegionId",
                table: "Rankings");

            migrationBuilder.CreateTable(
                name: "BrokerRegions",
                columns: table => new
                {
                    BrokerId = table.Column<int>(nullable: false),
                    RegionId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerRegions", x => new { x.BrokerId, x.RegionId });
                    table.ForeignKey(
                        name: "FK_BrokerRegions_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "BrokerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BrokerRegions_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "RegionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_BrokerId_RegionId",
                table: "Rankings",
                columns: new[] { "BrokerId", "RegionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrokerRegions_RegionId",
                table: "BrokerRegions",
                column: "RegionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rankings_BrokerRegions_BrokerId_RegionId",
                table: "Rankings",
                columns: new[] { "BrokerId", "RegionId" },
                principalTable: "BrokerRegions",
                principalColumns: new[] { "BrokerId", "RegionId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
