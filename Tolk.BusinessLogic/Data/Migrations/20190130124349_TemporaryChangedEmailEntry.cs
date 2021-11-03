using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class TemporaryChangedEmailEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemporaryChangedEmailStoreEntries",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    EmailAddress = table.Column<string>(nullable: false),
                    ExpirationDate = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryChangedEmailStoreEntries", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_TemporaryChangedEmailStoreEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemporaryChangedEmailStoreEntries");
        }
    }
}
