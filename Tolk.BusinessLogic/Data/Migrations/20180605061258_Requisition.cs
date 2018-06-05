using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class Requisition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Requisition",
                columns: table => new
                {
                    RequisitionId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    CreatedBy = table.Column<int>(nullable: false),
                    ImpersonatingCreatedBy = table.Column<int>(nullable: true),
                    TravelCosts = table.Column<decimal>(type: "decimal(10, 2)", nullable: false),
                    Status = table.Column<int>(nullable: false),
                    TimeWasteBeforeStartedAt = table.Column<DateTimeOffset>(nullable: true),
                    SessionStartedAt = table.Column<DateTimeOffset>(nullable: false),
                    SessionEndedAt = table.Column<DateTimeOffset>(nullable: false),
                    TimeWasteAfterEndedAt = table.Column<DateTimeOffset>(nullable: true),
                    Message = table.Column<string>(maxLength: 1000, nullable: false),
                    DenyMessage = table.Column<string>(maxLength: 255, nullable: true),
                    RequestId = table.Column<int>(nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(nullable: true),
                    ProcessedBy = table.Column<int>(nullable: true),
                    ImpersonatingProcessedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requisition", x => x.RequisitionId);
                    table.ForeignKey(
                        name: "FK_Requisition_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requisition_AspNetUsers_ImpersonatingCreatedBy",
                        column: x => x.ImpersonatingCreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requisition_AspNetUsers_ImpersonatingProcessedBy",
                        column: x => x.ImpersonatingProcessedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requisition_AspNetUsers_ProcessedBy",
                        column: x => x.ProcessedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requisition_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Requisition_CreatedBy",
                table: "Requisition",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Requisition_ImpersonatingCreatedBy",
                table: "Requisition",
                column: "ImpersonatingCreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Requisition_ImpersonatingProcessedBy",
                table: "Requisition",
                column: "ImpersonatingProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Requisition_ProcessedBy",
                table: "Requisition",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Requisition_RequestId",
                table: "Requisition",
                column: "RequestId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Requisition");
        }
    }
}
