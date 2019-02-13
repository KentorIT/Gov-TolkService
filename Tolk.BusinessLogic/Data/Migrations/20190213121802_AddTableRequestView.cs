using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddTableRequestView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestViews",
                columns: table => new
                {
                    RequestViewId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RequestId = table.Column<int>(nullable: false),
                    ViewedAt = table.Column<DateTimeOffset>(nullable: false),
                    ViewedBy = table.Column<int>(nullable: false),
                    ImpersonatingViewedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestViews", x => x.RequestViewId);
                    table.ForeignKey(
                        name: "FK_RequestViews_AspNetUsers_ImpersonatingViewedBy",
                        column: x => x.ImpersonatingViewedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestViews_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestViews_AspNetUsers_ViewedBy",
                        column: x => x.ViewedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestViews_ImpersonatingViewedBy",
                table: "RequestViews",
                column: "ImpersonatingViewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestViews_RequestId",
                table: "RequestViews",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestViews_ViewedBy",
                table: "RequestViews",
                column: "ViewedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestViews");
        }
    }
}
