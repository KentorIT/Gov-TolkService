using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddConfirmationsForOrderReqs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ExpectedTravelCosts",
                table: "Requests",
                type: "decimal(10, 2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompetenceLevel",
                table: "Requests",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderRequirementRequestAnswer",
                columns: table => new
                {
                    Answer = table.Column<string>(maxLength: 1000, nullable: true),
                    CanSatisfyRequirement = table.Column<bool>(nullable: false),
                    OrderRequirementId = table.Column<int>(nullable: false),
                    RequestId = table.Column<int>(nullable: false),
                    RequestId1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRequirementRequestAnswer", x => new { x.RequestId, x.OrderRequirementId });
                    table.ForeignKey(
                        name: "FK_OrderRequirementRequestAnswer_OrderRequirements_OrderRequirementId",
                        column: x => x.OrderRequirementId,
                        principalTable: "OrderRequirements",
                        principalColumn: "OrderRequirementId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderRequirementRequestAnswer_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderRequirementRequestAnswer_Requests_RequestId1",
                        column: x => x.RequestId1,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderRequirementRequestAnswer_OrderRequirementId",
                table: "OrderRequirementRequestAnswer",
                column: "OrderRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRequirementRequestAnswer_RequestId1",
                table: "OrderRequirementRequestAnswer",
                column: "RequestId1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderRequirementRequestAnswer");

            migrationBuilder.DropColumn(
                name: "CompetenceLevel",
                table: "Requests");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExpectedTravelCosts",
                table: "Requests",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10, 2)",
                oldNullable: true);
        }
    }
}
