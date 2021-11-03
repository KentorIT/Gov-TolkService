using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderContactPersonHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderContactPersonHistory",
                columns: table => new
                {
                    OrderContactPersonHistoryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    OrderId = table.Column<int>(nullable: false),
                    PreviousContactPersonId = table.Column<int>(nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(nullable: false),
                    ChangedBy = table.Column<int>(nullable: false),
                    ImpersonatingChangeUserId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderContactPersonHistory", x => x.OrderContactPersonHistoryId);
                    table.ForeignKey(
                        name: "FK_OrderContactPersonHistory_AspNetUsers_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderContactPersonHistory_AspNetUsers_ImpersonatingChangeUserId",
                        column: x => x.ImpersonatingChangeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderContactPersonHistory_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderContactPersonHistory_AspNetUsers_PreviousContactPersonId",
                        column: x => x.PreviousContactPersonId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderContactPersonHistory_ChangedBy",
                table: "OrderContactPersonHistory",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderContactPersonHistory_ImpersonatingChangeUserId",
                table: "OrderContactPersonHistory",
                column: "ImpersonatingChangeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderContactPersonHistory_OrderId",
                table: "OrderContactPersonHistory",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderContactPersonHistory_PreviousContactPersonId",
                table: "OrderContactPersonHistory",
                column: "PreviousContactPersonId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderContactPersonHistory");
        }
    }
}
