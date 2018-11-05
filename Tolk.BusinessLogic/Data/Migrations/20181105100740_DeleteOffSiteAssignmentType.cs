using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class DeleteOffSiteAssignmentType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OffSiteAssignmentType",
                table: "OrderInterpreterLocation");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OffSiteAssignmentType",
                table: "OrderInterpreterLocation",
                nullable: true);
        }
    }
}
