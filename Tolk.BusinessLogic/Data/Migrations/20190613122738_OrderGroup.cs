using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderGroupId",
                table: "Orders",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderGroups",
                columns: table => new
                {
                    OrderGroupId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    OrderGroupNumber = table.Column<string>(maxLength: 255, nullable: false, computedColumnSql: "'G-' + CAST(YEAR([CreatedAt]) AS NVARCHAR(100)) + '-' + CAST(([OrderGroupId]+(100000)) AS NVARCHAR(100))"),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    CreatedBy = table.Column<int>(nullable: false),
                    ImpersonatingCreator = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderGroups", x => x.OrderGroupId);
                    table.ForeignKey(
                        name: "FK_OrderGroups_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderGroups_AspNetUsers_ImpersonatingCreator",
                        column: x => x.ImpersonatingCreator,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderGroupId",
                table: "Orders",
                column: "OrderGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroups_CreatedBy",
                table: "OrderGroups",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderGroups_ImpersonatingCreator",
                table: "OrderGroups",
                column: "ImpersonatingCreator");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_OrderGroups_OrderGroupId",
                table: "Orders",
                column: "OrderGroupId",
                principalTable: "OrderGroups",
                principalColumn: "OrderGroupId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_OrderGroups_OrderGroupId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "OrderGroups");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderGroupId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderGroupId",
                table: "Orders");
        }
    }
}
