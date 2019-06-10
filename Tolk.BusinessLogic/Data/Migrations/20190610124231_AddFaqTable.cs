using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddFaqTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Faq",
                columns: table => new
                {
                    FaqId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    CreatedBy = table.Column<int>(nullable: false),
                    Question = table.Column<string>(maxLength: 255, nullable: true),
                    Answer = table.Column<string>(maxLength: 2000, nullable: true),
                    IsDisplayed = table.Column<bool>(nullable: false),
                    LastUpdatedBy = table.Column<int>(nullable: true),
                    LastUpdatedAt = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faq", x => x.FaqId);
                    table.ForeignKey(
                        name: "FK_Faq_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Faq_AspNetUsers_LastUpdatedBy",
                        column: x => x.LastUpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FaqDisplayUserRole",
                columns: table => new
                {
                    FaqId = table.Column<int>(nullable: false),
                    DisplayUserRole = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqDisplayUserRole", x => new { x.FaqId, x.DisplayUserRole });
                    table.ForeignKey(
                        name: "FK_FaqDisplayUserRole_Faq_FaqId",
                        column: x => x.FaqId,
                        principalTable: "Faq",
                        principalColumn: "FaqId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Faq_CreatedBy",
                table: "Faq",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Faq_LastUpdatedBy",
                table: "Faq",
                column: "LastUpdatedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaqDisplayUserRole");

            migrationBuilder.DropTable(
                name: "Faq");
        }
    }
}
