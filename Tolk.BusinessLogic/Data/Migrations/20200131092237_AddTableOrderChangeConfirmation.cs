using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddTableOrderChangeConfirmation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderChangeConfirmations",
                columns: table => new
                {
                    OrderChangeConfirmationId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ConfirmedAt = table.Column<DateTimeOffset>(nullable: false),
                    ConfirmedBy = table.Column<int>(nullable: false),
                    ImpersonatingConfirmedBy = table.Column<int>(nullable: true),
                    OrderChangeLogEntryId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderChangeConfirmations", x => x.OrderChangeConfirmationId);
                    table.ForeignKey(
                        name: "FK_OrderChangeConfirmations_AspNetUsers_ConfirmedBy",
                        column: x => x.ConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderChangeConfirmations_AspNetUsers_ImpersonatingConfirmedBy",
                        column: x => x.ImpersonatingConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderChangeConfirmations_OrderChangeLogEntries_OrderChangeLogEntryId",
                        column: x => x.OrderChangeLogEntryId,
                        principalTable: "OrderChangeLogEntries",
                        principalColumn: "OrderChangeLogEntryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeConfirmations_ConfirmedBy",
                table: "OrderChangeConfirmations",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeConfirmations_ImpersonatingConfirmedBy",
                table: "OrderChangeConfirmations",
                column: "ImpersonatingConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeConfirmations_OrderChangeLogEntryId",
                table: "OrderChangeConfirmations",
                column: "OrderChangeLogEntryId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderChangeConfirmations");
        }
    }
}
