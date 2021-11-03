using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RequestGroupExpiryLogTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestGroupUpdateLatestAnswerTime",
                columns: table => new
                {
                    RequestGroupId = table.Column<int>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedBy = table.Column<int>(nullable: false),
                    ImpersonatorUpdatedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestGroupUpdateLatestAnswerTime", x => x.RequestGroupId);
                    table.ForeignKey(
                        name: "FK_RequestGroupUpdateLatestAnswerTime_AspNetUsers_ImpersonatorUpdatedBy",
                        column: x => x.ImpersonatorUpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestGroupUpdateLatestAnswerTime_RequestGroups_RequestGroupId",
                        column: x => x.RequestGroupId,
                        principalTable: "RequestGroups",
                        principalColumn: "RequestGroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestGroupUpdateLatestAnswerTime_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroupUpdateLatestAnswerTime_ImpersonatorUpdatedBy",
                table: "RequestGroupUpdateLatestAnswerTime",
                column: "ImpersonatorUpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroupUpdateLatestAnswerTime_UpdatedBy",
                table: "RequestGroupUpdateLatestAnswerTime",
                column: "UpdatedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestGroupUpdateLatestAnswerTime");
        }
    }
}
