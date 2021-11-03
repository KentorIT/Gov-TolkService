using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class NewTableRequestStatusConfirmation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestStatusConfirmation",
                columns: table => new
                {
                    RequestStatusConfirmationId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RequestId = table.Column<int>(nullable: false),
                    RequestStatus = table.Column<int>(nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(nullable: true),
                    ConfirmedBy = table.Column<int>(nullable: true),
                    ImpersonatingConfirmedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestStatusConfirmation", x => x.RequestStatusConfirmationId);
                    table.ForeignKey(
                        name: "FK_RequestStatusConfirmation_AspNetUsers_ConfirmedBy",
                        column: x => x.ConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestStatusConfirmation_AspNetUsers_ImpersonatingConfirmedBy",
                        column: x => x.ImpersonatingConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestStatusConfirmation_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestStatusConfirmation_ConfirmedBy",
                table: "RequestStatusConfirmation",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestStatusConfirmation_ImpersonatingConfirmedBy",
                table: "RequestStatusConfirmation",
                column: "ImpersonatingConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestStatusConfirmation_RequestId",
                table: "RequestStatusConfirmation",
                column: "RequestId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestStatusConfirmation");
        }
    }
}
