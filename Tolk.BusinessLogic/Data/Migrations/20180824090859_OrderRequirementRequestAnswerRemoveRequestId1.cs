using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderRequirementRequestAnswerRemoveRequestId1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderRequirementRequestAnswer_Requests_RequestId1",
                table: "OrderRequirementRequestAnswer");

            migrationBuilder.DropIndex(
                name: "IX_OrderRequirementRequestAnswer_RequestId1",
                table: "OrderRequirementRequestAnswer");

            migrationBuilder.DropColumn(
                name: "RequestId1",
                table: "OrderRequirementRequestAnswer");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestId1",
                table: "OrderRequirementRequestAnswer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderRequirementRequestAnswer_RequestId1",
                table: "OrderRequirementRequestAnswer",
                column: "RequestId1");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderRequirementRequestAnswer_Requests_RequestId1",
                table: "OrderRequirementRequestAnswer",
                column: "RequestId1",
                principalTable: "Requests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
