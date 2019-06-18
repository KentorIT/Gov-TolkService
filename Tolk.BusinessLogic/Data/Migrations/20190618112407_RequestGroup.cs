using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RequestGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestGroupId",
                table: "Requests",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RequestGroups",
                columns: table => new
                {
                    RequestGroupId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RankingId = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    BrokerMessage = table.Column<string>(maxLength: 1000, nullable: true),
                    DenyMessage = table.Column<string>(maxLength: 1000, nullable: true),
                    RecievedAt = table.Column<DateTimeOffset>(nullable: true),
                    ReceivedBy = table.Column<int>(nullable: true),
                    ImpersonatingReceivedBy = table.Column<int>(nullable: true),
                    AnswerDate = table.Column<DateTimeOffset>(nullable: true),
                    AnsweredBy = table.Column<int>(nullable: true),
                    ImpersonatingAnsweredBy = table.Column<int>(nullable: true),
                    AnswerProcessedAt = table.Column<DateTimeOffset>(nullable: true),
                    AnswerProcessedBy = table.Column<int>(nullable: true),
                    ImpersonatingAnswerProcessedBy = table.Column<int>(nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(nullable: true),
                    CancelledBy = table.Column<int>(nullable: true),
                    ImpersonatingCanceller = table.Column<int>(nullable: true),
                    CancelMessage = table.Column<string>(maxLength: 1000, nullable: true),
                    IsTerminalRequest = table.Column<bool>(nullable: false),
                    OrderGroupId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestGroups", x => x.RequestGroupId);
                    table.ForeignKey(
                        name: "FK_RequestGroups_AspNetUsers_AnswerProcessedBy",
                        column: x => x.AnswerProcessedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestGroups_AspNetUsers_AnsweredBy",
                        column: x => x.AnsweredBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestGroups_AspNetUsers_CancelledBy",
                        column: x => x.CancelledBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestGroups_AspNetUsers_ImpersonatingAnswerProcessedBy",
                        column: x => x.ImpersonatingAnswerProcessedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestGroups_AspNetUsers_ImpersonatingAnsweredBy",
                        column: x => x.ImpersonatingAnsweredBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestGroups_AspNetUsers_ImpersonatingCanceller",
                        column: x => x.ImpersonatingCanceller,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestGroups_AspNetUsers_ImpersonatingReceivedBy",
                        column: x => x.ImpersonatingReceivedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestGroups_OrderGroups_OrderGroupId",
                        column: x => x.OrderGroupId,
                        principalTable: "OrderGroups",
                        principalColumn: "OrderGroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestGroups_Rankings_RankingId",
                        column: x => x.RankingId,
                        principalTable: "Rankings",
                        principalColumn: "RankingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestGroups_AspNetUsers_ReceivedBy",
                        column: x => x.ReceivedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RequestGroupId",
                table: "Requests",
                column: "RequestGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_AnswerProcessedBy",
                table: "RequestGroups",
                column: "AnswerProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_AnsweredBy",
                table: "RequestGroups",
                column: "AnsweredBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_CancelledBy",
                table: "RequestGroups",
                column: "CancelledBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_ImpersonatingAnswerProcessedBy",
                table: "RequestGroups",
                column: "ImpersonatingAnswerProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_ImpersonatingAnsweredBy",
                table: "RequestGroups",
                column: "ImpersonatingAnsweredBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_ImpersonatingCanceller",
                table: "RequestGroups",
                column: "ImpersonatingCanceller");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_ImpersonatingReceivedBy",
                table: "RequestGroups",
                column: "ImpersonatingReceivedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_OrderGroupId",
                table: "RequestGroups",
                column: "OrderGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_RankingId",
                table: "RequestGroups",
                column: "RankingId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroups_ReceivedBy",
                table: "RequestGroups",
                column: "ReceivedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_RequestGroups_RequestGroupId",
                table: "Requests",
                column: "RequestGroupId",
                principalTable: "RequestGroups",
                principalColumn: "RequestGroupId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_RequestGroups_RequestGroupId",
                table: "Requests");

            migrationBuilder.DropTable(
                name: "RequestGroups");

            migrationBuilder.DropIndex(
                name: "IX_Requests_RequestGroupId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RequestGroupId",
                table: "Requests");
        }
    }
}
