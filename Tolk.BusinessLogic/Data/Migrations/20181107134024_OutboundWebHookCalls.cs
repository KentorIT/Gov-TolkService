using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OutboundWebHookCalls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboundWebHookCalls",
                columns: table => new
                {
                    OutboundWebHookCallId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RecipientUrl = table.Column<string>(nullable: false),
                    Payload = table.Column<string>(nullable: false),
                    NotificationType = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    RecipientUserId = table.Column<int>(nullable: false),
                    DeliveredAt = table.Column<DateTimeOffset>(nullable: true),
                    FailedTries = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundWebHookCalls", x => x.OutboundWebHookCallId);
                    table.ForeignKey(
                        name: "FK_OutboundWebHookCalls_AspNetUsers_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundWebHookCalls_RecipientUserId",
                table: "OutboundWebHookCalls",
                column: "RecipientUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboundWebHookCalls");
        }
    }
}
