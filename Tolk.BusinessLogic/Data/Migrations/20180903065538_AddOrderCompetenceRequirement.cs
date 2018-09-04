using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddOrderCompetenceRequirement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredCompetenceLevel",
                table: "Orders");

            migrationBuilder.AddColumn<bool>(
                name: "SpecificCompetenceLevelRequired",
                table: "Orders",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "OrderCompetenceRequirements",
                columns: table => new
                {
                    OrderCompetenceRequirementId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CompetenceLevel = table.Column<int>(nullable: false),
                    Rank = table.Column<int>(nullable: true),
                    OrderId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderCompetenceRequirements", x => x.OrderCompetenceRequirementId);
                    table.ForeignKey(
                        name: "FK_OrderCompetenceRequirements_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderCompetenceRequirements_OrderId",
                table: "OrderCompetenceRequirements",
                column: "OrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderCompetenceRequirements");

            migrationBuilder.DropColumn(
                name: "SpecificCompetenceLevelRequired",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "RequiredCompetenceLevel",
                table: "Orders",
                nullable: false,
                defaultValue: 0);
        }
    }
}
