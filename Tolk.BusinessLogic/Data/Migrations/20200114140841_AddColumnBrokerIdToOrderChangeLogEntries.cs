using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddColumnBrokerIdToOrderChangeLogEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BrokerId",
                table: "OrderChangeLogEntries",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeLogEntries_BrokerId",
                table: "OrderChangeLogEntries",
                column: "BrokerId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderChangeLogEntries_Brokers_BrokerId",
                table: "OrderChangeLogEntries",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "BrokerId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderChangeLogEntries_Brokers_BrokerId",
                table: "OrderChangeLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_OrderChangeLogEntries_BrokerId",
                table: "OrderChangeLogEntries");

            migrationBuilder.DropColumn(
                name: "BrokerId",
                table: "OrderChangeLogEntries");
        }
    }
}
