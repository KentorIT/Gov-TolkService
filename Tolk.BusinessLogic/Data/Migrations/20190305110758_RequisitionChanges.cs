using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RequisitionChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DenyMessage",
                table: "Requisitions",
                newName: "CustomerComment");

            migrationBuilder.AddColumn<int>(
                name: "CarCompensation",
                table: "Requisitions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerDiem",
                table: "Requisitions",
                maxLength: 1000,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarCompensation",
                table: "Requisitions");

            migrationBuilder.DropColumn(
                name: "PerDiem",
                table: "Requisitions");

            migrationBuilder.RenameColumn(
                name: "CustomerComment",
                table: "Requisitions",
                newName: "DenyMessage");
        }
    }
}
