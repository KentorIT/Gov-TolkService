using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AdditionalUserInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameFamily",
                table: "AspNetUsers",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameFirst",
                table: "AspNetUsers",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumberCellphone",
                table: "AspNetUsers",
                maxLength: 32,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameFamily",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NameFirst",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PhoneNumberCellphone",
                table: "AspNetUsers");
        }
    }
}
