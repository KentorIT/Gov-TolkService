using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderChangeUpdatedByNotNull : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderChangeLogEntries_AspNetUsers_UpdatedByUserId",
                table: "OrderChangeLogEntries");

            migrationBuilder.AlterColumn<int>(
                name: "UpdatedByUserId",
                table: "OrderChangeLogEntries",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderChangeLogEntries_AspNetUsers_UpdatedByUserId",
                table: "OrderChangeLogEntries",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderChangeLogEntries_AspNetUsers_UpdatedByUserId",
                table: "OrderChangeLogEntries");

            migrationBuilder.AlterColumn<int>(
                name: "UpdatedByUserId",
                table: "OrderChangeLogEntries",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddForeignKey(
                name: "FK_OrderChangeLogEntries_AspNetUsers_UpdatedByUserId",
                table: "OrderChangeLogEntries",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
