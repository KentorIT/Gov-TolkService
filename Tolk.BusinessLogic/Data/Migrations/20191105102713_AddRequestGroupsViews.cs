using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddRequestGroupsViews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestGroupViews",
                columns: table => new
                {
                    RequestGroupViewId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ViewedAt = table.Column<DateTimeOffset>(nullable: false),
                    ViewedBy = table.Column<int>(nullable: false),
                    ImpersonatingViewedBy = table.Column<int>(nullable: true),
                    RequestGroupId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestGroupViews", x => x.RequestGroupViewId);
                    table.ForeignKey(
                        name: "FK_RequestGroupViews_AspNetUsers_ImpersonatingViewedBy",
                        column: x => x.ImpersonatingViewedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestGroupViews_RequestGroups_RequestGroupId",
                        column: x => x.RequestGroupId,
                        principalTable: "RequestGroups",
                        principalColumn: "RequestGroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestGroupViews_AspNetUsers_ViewedBy",
                        column: x => x.ViewedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroupViews_RequestGroupId",
                table: "RequestGroupViews",
                column: "RequestGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroupViews_ViewedBy",
                table: "RequestGroupViews",
                column: "ViewedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestGroupViews");
        }
    }
}
