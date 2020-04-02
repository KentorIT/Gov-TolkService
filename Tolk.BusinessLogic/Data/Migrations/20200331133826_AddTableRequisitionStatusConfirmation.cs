using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddTableRequisitionStatusConfirmation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequisitionStatusConfirmations",
                columns: table => new
                {
                    RequisitionStatusConfirmationId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ConfirmedAt = table.Column<DateTimeOffset>(nullable: false),
                    ConfirmedBy = table.Column<int>(nullable: false),
                    ImpersonatingConfirmedBy = table.Column<int>(nullable: true),
                    RequisitionId = table.Column<int>(nullable: false),
                    RequisitionStatus = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionStatusConfirmations", x => x.RequisitionStatusConfirmationId);
                    table.ForeignKey(
                        name: "FK_RequisitionStatusConfirmations_AspNetUsers_ConfirmedBy",
                        column: x => x.ConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisitionStatusConfirmations_AspNetUsers_ImpersonatingConfirmedBy",
                        column: x => x.ImpersonatingConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequisitionStatusConfirmations_Requisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "Requisitions",
                        principalColumn: "RequisitionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionStatusConfirmations_ConfirmedBy",
                table: "RequisitionStatusConfirmations",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionStatusConfirmations_ImpersonatingConfirmedBy",
                table: "RequisitionStatusConfirmations",
                column: "ImpersonatingConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionStatusConfirmations_RequisitionId",
                table: "RequisitionStatusConfirmations",
                column: "RequisitionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequisitionStatusConfirmations");
        }
    }
}
