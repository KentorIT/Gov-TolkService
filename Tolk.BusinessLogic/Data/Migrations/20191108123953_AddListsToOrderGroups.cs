using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddListsToOrderGroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderGroupCompetenceRequirements",
                columns: table => new
                {
                    CompetenceLevel = table.Column<int>(nullable: false),
                    OrderGroupId = table.Column<int>(nullable: false),
                    Rank = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderGroupCompetenceRequirements", x => new { x.OrderGroupId, x.CompetenceLevel });
                    table.ForeignKey(
                        name: "FK_OrderGroupCompetenceRequirements_OrderGroups_OrderGroupId",
                        column: x => x.OrderGroupId,
                        principalTable: "OrderGroups",
                        principalColumn: "OrderGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderGroupInterpreterLocations",
                columns: table => new
                {
                    InterpreterLocation = table.Column<int>(nullable: false),
                    OrderGroupId = table.Column<int>(nullable: false),
                    Rank = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderGroupInterpreterLocations", x => new { x.OrderGroupId, x.InterpreterLocation });
                    table.ForeignKey(
                        name: "FK_OrderGroupInterpreterLocations_OrderGroups_OrderGroupId",
                        column: x => x.OrderGroupId,
                        principalTable: "OrderGroups",
                        principalColumn: "OrderGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderGroupRequirements",
                columns: table => new
                {
                    OrderGroupRequirementId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RequirementType = table.Column<int>(nullable: false),
                    Description = table.Column<string>(maxLength: 1000, nullable: true),
                    IsRequired = table.Column<bool>(nullable: false),
                    OrderGroupId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderGroupRequirements", x => x.OrderGroupRequirementId);
                    table.ForeignKey(
                        name: "FK_OrderGroupRequirements_OrderGroups_OrderGroupId",
                        column: x => x.OrderGroupId,
                        principalTable: "OrderGroups",
                        principalColumn: "OrderGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroupRequirements_OrderGroupId",
                table: "OrderGroupRequirements",
                column: "OrderGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderGroupCompetenceRequirements");

            migrationBuilder.DropTable(
                name: "OrderGroupInterpreterLocations");

            migrationBuilder.DropTable(
                name: "OrderGroupRequirements");
        }
    }
}
