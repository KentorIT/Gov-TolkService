using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddTableOrderStatusConfirmation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderStatusConfirmation",
                columns: table => new
                {
                    OrderStatusConfirmationId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    OrderId = table.Column<int>(nullable: false),
                    OrderStatus = table.Column<int>(nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(nullable: true),
                    ConfirmedBy = table.Column<int>(nullable: true),
                    ImpersonatingConfirmedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusConfirmation", x => x.OrderStatusConfirmationId);
                    table.ForeignKey(
                        name: "FK_OrderStatusConfirmation_AspNetUsers_ConfirmedBy",
                        column: x => x.ConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderStatusConfirmation_AspNetUsers_ImpersonatingConfirmedBy",
                        column: x => x.ImpersonatingConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderStatusConfirmation_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusConfirmation_ConfirmedBy",
                table: "OrderStatusConfirmation",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusConfirmation_ImpersonatingConfirmedBy",
                table: "OrderStatusConfirmation",
                column: "ImpersonatingConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusConfirmation_OrderId",
                table: "OrderStatusConfirmation",
                column: "OrderId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderStatusConfirmation");
        }
    }
}
