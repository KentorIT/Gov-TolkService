using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class MovedAddressAndOffSiteinfoToOrderInterpreterLocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OffSiteAssignmentType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OffSiteContactInformation",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "OrderInterpreterLocation",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OffSiteAssignmentType",
                table: "OrderInterpreterLocation",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OffSiteContactInformation",
                table: "OrderInterpreterLocation",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "OrderInterpreterLocation",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "OrderInterpreterLocation",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "OrderInterpreterLocation");

            migrationBuilder.DropColumn(
                name: "OffSiteAssignmentType",
                table: "OrderInterpreterLocation");

            migrationBuilder.DropColumn(
                name: "OffSiteContactInformation",
                table: "OrderInterpreterLocation");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "OrderInterpreterLocation");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "OrderInterpreterLocation");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Orders",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OffSiteAssignmentType",
                table: "Orders",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OffSiteContactInformation",
                table: "Orders",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Orders",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Orders",
                maxLength: 100,
                nullable: true);
        }
    }
}
