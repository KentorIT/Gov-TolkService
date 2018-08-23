using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddComplaint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Complaints",
                columns: table => new
                {
                    ComplaintId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Status = table.Column<int>(nullable: false),
                    ComplaintType = table.Column<int>(nullable: false),
                    RequestId = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    CreatedBy = table.Column<int>(nullable: false),
                    ImpersonatingCreatedBy = table.Column<int>(nullable: true),
                    ComplaintMessage = table.Column<string>(maxLength: 1000, nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(nullable: true),
                    AnsweredBy = table.Column<int>(nullable: true),
                    ImpersonatingAnsweredBy = table.Column<int>(nullable: true),
                    AnswerMessage = table.Column<string>(maxLength: 1000, nullable: true),
                    AnswerDisputedAt = table.Column<DateTimeOffset>(nullable: true),
                    AnswerDisputedBy = table.Column<int>(nullable: true),
                    ImpersonatingAnswerDisputedBy = table.Column<int>(nullable: true),
                    AnswerDisputedMessage = table.Column<string>(maxLength: 1000, nullable: true),
                    TerminatedAt = table.Column<DateTimeOffset>(nullable: true),
                    TerminatedBy = table.Column<int>(nullable: true),
                    ImpersonatingTerminatedBy = table.Column<int>(nullable: true),
                    TerminationMessage = table.Column<string>(maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Complaints", x => x.ComplaintId);
                    table.ForeignKey(
                        name: "FK_Complaints_AspNetUsers_AnswerDisputedBy",
                        column: x => x.AnswerDisputedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Complaints_AspNetUsers_AnsweredBy",
                        column: x => x.AnsweredBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Complaints_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Complaints_AspNetUsers_ImpersonatingAnswerDisputedBy",
                        column: x => x.ImpersonatingAnswerDisputedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Complaints_AspNetUsers_ImpersonatingAnsweredBy",
                        column: x => x.ImpersonatingAnsweredBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Complaints_AspNetUsers_ImpersonatingCreatedBy",
                        column: x => x.ImpersonatingCreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Complaints_AspNetUsers_ImpersonatingTerminatedBy",
                        column: x => x.ImpersonatingTerminatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Complaints_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Complaints_AspNetUsers_TerminatedBy",
                        column: x => x.TerminatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_AnswerDisputedBy",
                table: "Complaints",
                column: "AnswerDisputedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_AnsweredBy",
                table: "Complaints",
                column: "AnsweredBy");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_CreatedBy",
                table: "Complaints",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_ImpersonatingAnswerDisputedBy",
                table: "Complaints",
                column: "ImpersonatingAnswerDisputedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_ImpersonatingAnsweredBy",
                table: "Complaints",
                column: "ImpersonatingAnsweredBy");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_ImpersonatingCreatedBy",
                table: "Complaints",
                column: "ImpersonatingCreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_ImpersonatingTerminatedBy",
                table: "Complaints",
                column: "ImpersonatingTerminatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_RequestId",
                table: "Complaints",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_TerminatedBy",
                table: "Complaints",
                column: "TerminatedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Complaints");
        }
    }
}
