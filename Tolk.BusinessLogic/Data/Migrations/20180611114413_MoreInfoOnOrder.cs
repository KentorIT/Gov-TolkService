using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class MoreInfoOnOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtherAddressInformation",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OtherContactEmail",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OtherContactPhone",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "StartDateTime",
                table: "Orders",
                newName: "StartAt");

            migrationBuilder.RenameColumn(
                name: "OtherContactPerson",
                table: "Orders",
                newName: "OffSiteContactInformation");

            migrationBuilder.RenameColumn(
                name: "EndDateTime",
                table: "Orders",
                newName: "EndAt");

            migrationBuilder.AddColumn<int>(
                name: "ContactPersonId",
                table: "Orders",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OffSiteAssignmentType",
                table: "Orders",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ContactPersonId",
                table: "Orders",
                column: "ContactPersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_ContactPersonId",
                table: "Orders",
                column: "ContactPersonId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_ContactPersonId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ContactPersonId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ContactPersonId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OffSiteAssignmentType",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "StartAt",
                table: "Orders",
                newName: "StartDateTime");

            migrationBuilder.RenameColumn(
                name: "OffSiteContactInformation",
                table: "Orders",
                newName: "OtherContactPerson");

            migrationBuilder.RenameColumn(
                name: "EndAt",
                table: "Orders",
                newName: "EndDateTime");

            migrationBuilder.AddColumn<string>(
                name: "OtherAddressInformation",
                table: "Orders",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherContactEmail",
                table: "Orders",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherContactPhone",
                table: "Orders",
                maxLength: 50,
                nullable: true);
        }
    }
}
