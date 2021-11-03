using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddWebHookLogging : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResentHookId",
                table: "OutboundWebHookCalls",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FailedWebHookCalls",
                columns: table => new
                {
                    FailedWebHookCallId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    OutboundWebHookCallId = table.Column<int>(nullable: false),
                    FailedAt = table.Column<DateTimeOffset>(nullable: false),
                    ErrorMessage = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedWebHookCalls", x => x.FailedWebHookCallId);
                    table.ForeignKey(
                        name: "FK_FailedWebHookCalls_OutboundWebHookCalls_OutboundWebHookCallId",
                        column: x => x.OutboundWebHookCallId,
                        principalTable: "OutboundWebHookCalls",
                        principalColumn: "OutboundWebHookCallId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FailedWebHookCalls_OutboundWebHookCallId",
                table: "FailedWebHookCalls",
                column: "OutboundWebHookCallId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailedWebHookCalls");

            migrationBuilder.DropColumn(
                name: "ResentHookId",
                table: "OutboundWebHookCalls");
        }
    }
}
