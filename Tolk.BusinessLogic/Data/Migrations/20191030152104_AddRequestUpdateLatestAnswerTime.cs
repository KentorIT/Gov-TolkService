using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddRequestUpdateLatestAnswerTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestUpdateLatestAnswerTime",
                columns: table => new
                {
                    RequestId = table.Column<int>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedBy = table.Column<int>(nullable: false),
                    ImpersonatorUpdatedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestUpdateLatestAnswerTime", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_RequestUpdateLatestAnswerTime_AspNetUsers_ImpersonatorUpdatedBy",
                        column: x => x.ImpersonatorUpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestUpdateLatestAnswerTime_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestUpdateLatestAnswerTime_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestUpdateLatestAnswerTime_ImpersonatorUpdatedBy",
                table: "RequestUpdateLatestAnswerTime",
                column: "ImpersonatorUpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestUpdateLatestAnswerTime_UpdatedBy",
                table: "RequestUpdateLatestAnswerTime",
                column: "UpdatedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestUpdateLatestAnswerTime");
        }
    }
}
