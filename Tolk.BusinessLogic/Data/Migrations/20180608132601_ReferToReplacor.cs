using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class ReferToReplacor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReplacedByRequisitionId",
                table: "Requisitions",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requisitions_ReplacedByRequisitionId",
                table: "Requisitions",
                column: "ReplacedByRequisitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_Requisitions_ReplacedByRequisitionId",
                table: "Requisitions",
                column: "ReplacedByRequisitionId",
                principalTable: "Requisitions",
                principalColumn: "RequisitionId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_Requisitions_ReplacedByRequisitionId",
                table: "Requisitions");

            migrationBuilder.DropIndex(
                name: "IX_Requisitions_ReplacedByRequisitionId",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "ReplacedByRequisitionId",
                table: "Requisitions");
        }
    }
}
