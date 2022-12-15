using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddFrameworkAgreement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FrameworkAgreementId",
                table: "Rankings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FrameworkAgreements",
                columns: table => new
                {
                    FrameworkAgreementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgreementNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FirstValidDate = table.Column<DateTime>(type: "date", nullable: false),
                    LastValidDate = table.Column<DateTime>(type: "date", nullable: false),
                    BrokerFeeCalculationType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkAgreements", x => x.FrameworkAgreementId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_FrameworkAgreementId",
                table: "Rankings",
                column: "FrameworkAgreementId");

            //Add the first agreement
            migrationBuilder.Sql("exec('insert FrameworkAgreements Values(''23.3-9066-16'',''Första ramavtalet som tolkavropstjänsten hanterar'',''20180101'',''20230131'',1)')");
            // Add the agreement to all available rankings.
            migrationBuilder.Sql("Update Rankings Set FrameworkAgreementId = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_Rankings_FrameworkAgreements_FrameworkAgreementId",
                table: "Rankings",
                column: "FrameworkAgreementId",
                principalTable: "FrameworkAgreements",
                principalColumn: "FrameworkAgreementId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rankings_FrameworkAgreements_FrameworkAgreementId",
                table: "Rankings");

            migrationBuilder.DropTable(
                name: "FrameworkAgreements");

            migrationBuilder.DropIndex(
                name: "IX_Rankings_FrameworkAgreementId",
                table: "Rankings");

            migrationBuilder.DropColumn(
                name: "FrameworkAgreementId",
                table: "Rankings");
        }
    }
}
