using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddRegionGroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegionGroupId",
                table: "Regions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RegionGroups",
                columns: table => new
                {
                    RegionGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionGroups", x => x.RegionGroupId);
                });

            migrationBuilder.InsertData(
                table: "RegionGroups",
                columns: new[] { "RegionGroupId", "Name" },
                values: new object[] { 1, "Storstadsregioner" });

            migrationBuilder.InsertData(
                table: "RegionGroups",
                columns: new[] { "RegionGroupId", "Name" },
                values: new object[] { 2, "Norra mellansverige" });

            migrationBuilder.InsertData(
                table: "RegionGroups",
                columns: new[] { "RegionGroupId", "Name" },
                values: new object[] { 3, "Övriga" });

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 1,
                column: "RegionGroupId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 2,
                column: "RegionGroupId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 3,
                column: "RegionGroupId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 4,
                column: "RegionGroupId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 5,
                column: "RegionGroupId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 6,
                column: "RegionGroupId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 7,
                column: "RegionGroupId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 8,
                column: "RegionGroupId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 11,
                column: "RegionGroupId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 13,
                column: "RegionGroupId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 15,
                column: "RegionGroupId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 16,
                column: "RegionGroupId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 17,
                column: "RegionGroupId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 18,
                column: "RegionGroupId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 19,
                column: "RegionGroupId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 20,
                column: "RegionGroupId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 21,
                column: "RegionGroupId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 22,
                column: "RegionGroupId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 23,
                column: "RegionGroupId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 25,
                column: "RegionGroupId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Regions",
                keyColumn: "RegionId",
                keyValue: 80,
                column: "RegionGroupId",
                value: 3);

            migrationBuilder.CreateIndex(
                name: "IX_Regions_RegionGroupId",
                table: "Regions",
                column: "RegionGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Regions_RegionGroups_RegionGroupId",
                table: "Regions",
                column: "RegionGroupId",
                principalTable: "RegionGroups",
                principalColumn: "RegionGroupId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Regions_RegionGroups_RegionGroupId",
                table: "Regions");

            migrationBuilder.DropTable(
                name: "RegionGroups");

            migrationBuilder.DropIndex(
                name: "IX_Regions_RegionGroupId",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "RegionGroupId",
                table: "Regions");
        }
    }
}
