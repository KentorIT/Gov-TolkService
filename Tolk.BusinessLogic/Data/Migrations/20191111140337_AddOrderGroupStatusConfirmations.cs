using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddOrderGroupStatusConfirmations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderGroupStatusConfirmations",
                columns: table => new
                {
                    OrderGroupStatusConfirmationId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ConfirmedAt = table.Column<DateTimeOffset>(nullable: false),
                    ConfirmedBy = table.Column<int>(nullable: false),
                    ImpersonatingConfirmedBy = table.Column<int>(nullable: true),
                    OrderGroupId = table.Column<int>(nullable: false),
                    OrderStatus = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderGroupStatusConfirmations", x => x.OrderGroupStatusConfirmationId);
                    table.ForeignKey(
                        name: "FK_OrderGroupStatusConfirmations_AspNetUsers_ConfirmedBy",
                        column: x => x.ConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderGroupStatusConfirmations_AspNetUsers_ImpersonatingConfirmedBy",
                        column: x => x.ImpersonatingConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderGroupStatusConfirmations_OrderGroups_OrderGroupId",
                        column: x => x.OrderGroupId,
                        principalTable: "OrderGroups",
                        principalColumn: "OrderGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroupStatusConfirmations_ConfirmedBy",
                table: "OrderGroupStatusConfirmations",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroupStatusConfirmations_ImpersonatingConfirmedBy",
                table: "OrderGroupStatusConfirmations",
                column: "ImpersonatingConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroupStatusConfirmations_OrderGroupId",
                table: "OrderGroupStatusConfirmations",
                column: "OrderGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderGroupStatusConfirmations");
        }
    }
}
