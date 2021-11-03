using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddRequestGroupStatusConfirmations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestGroupStatusConfirmations",
                columns: table => new
                {
                    RequestGroupStatusConfirmationId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RequestGroupId = table.Column<int>(nullable: false),
                    RequestStatus = table.Column<int>(nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(nullable: false),
                    ConfirmedBy = table.Column<int>(nullable: false),
                    ImpersonatingConfirmedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestGroupStatusConfirmations", x => x.RequestGroupStatusConfirmationId);
                    table.ForeignKey(
                        name: "FK_RequestGroupStatusConfirmations_AspNetUsers_ConfirmedBy",
                        column: x => x.ConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestGroupStatusConfirmations_AspNetUsers_ImpersonatingConfirmedBy",
                        column: x => x.ImpersonatingConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestGroupStatusConfirmations_RequestGroups_RequestGroupId",
                        column: x => x.RequestGroupId,
                        principalTable: "RequestGroups",
                        principalColumn: "RequestGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroupStatusConfirmations_ConfirmedBy",
                table: "RequestGroupStatusConfirmations",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RequestGroupStatusConfirmations_RequestGroupId",
                table: "RequestGroupStatusConfirmations",
                column: "RequestGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestGroupStatusConfirmations");
        }
    }
}
