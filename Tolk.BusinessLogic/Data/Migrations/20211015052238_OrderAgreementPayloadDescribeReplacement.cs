using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderAgreementPayloadDescribeReplacement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderAgreementPayloads",
                table: "OrderAgreementPayloads");

            migrationBuilder.AddColumn<int>(
                name: "OrderAgreementPayloadId",
                table: "OrderAgreementPayloads",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "ReplacedById",
                table: "OrderAgreementPayloads",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderAgreementPayloads",
                table: "OrderAgreementPayloads",
                column: "OrderAgreementPayloadId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_ReplacedById",
                table: "OrderAgreementPayloads",
                column: "ReplacedById",
                unique: true,
                filter: "[ReplacedById] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAgreementPayloads_RequestId_Index",
                table: "OrderAgreementPayloads",
                columns: new[] { "RequestId", "Index" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAgreementPayloads_OrderAgreementPayloads_ReplacedById",
                table: "OrderAgreementPayloads",
                column: "ReplacedById",
                principalTable: "OrderAgreementPayloads",
                principalColumn: "OrderAgreementPayloadId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderAgreementPayloads_OrderAgreementPayloads_ReplacedById",
                table: "OrderAgreementPayloads");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderAgreementPayloads",
                table: "OrderAgreementPayloads");

            migrationBuilder.DropIndex(
                name: "IX_OrderAgreementPayloads_ReplacedById",
                table: "OrderAgreementPayloads");

            migrationBuilder.DropIndex(
                name: "IX_OrderAgreementPayloads_RequestId_Index",
                table: "OrderAgreementPayloads");

            migrationBuilder.DropColumn(
                name: "OrderAgreementPayloadId",
                table: "OrderAgreementPayloads");

            migrationBuilder.DropColumn(
                name: "ReplacedById",
                table: "OrderAgreementPayloads");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderAgreementPayloads",
                table: "OrderAgreementPayloads",
                columns: new[] { "RequestId", "Index" });
        }
    }
}
