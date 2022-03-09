using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddResendAndLoggToPeppolMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedTries",
                table: "OutboundPeppolMessages",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasNotifiedFailure",
                table: "OutboundPeppolMessages",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FailedPeppolMessages",
                columns: table => new
                {
                    FailedPeppolMessageId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutboundPeppolMessageId = table.Column<int>(nullable: false),
                    FailedAt = table.Column<DateTimeOffset>(nullable: false),
                    ErrorMessage = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedPeppolMessages", x => x.FailedPeppolMessageId);
                    table.ForeignKey(
                        name: "FK_FailedPeppolMessages_OutboundPeppolMessages_OutboundPeppolMessageId",
                        column: x => x.OutboundPeppolMessageId,
                        principalTable: "OutboundPeppolMessages",
                        principalColumn: "OutboundPeppolMessageId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FailedPeppolMessages_OutboundPeppolMessageId",
                table: "FailedPeppolMessages",
                column: "OutboundPeppolMessageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailedPeppolMessages");

            migrationBuilder.DropColumn(
                name: "FailedTries",
                table: "OutboundPeppolMessages");

            migrationBuilder.DropColumn(
                name: "HasNotifiedFailure",
                table: "OutboundPeppolMessages");
        }
    }
}
