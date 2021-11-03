using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class quarantine : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quarantines",
                columns: table => new
                {
                    QuarantineId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    CreatedBy = table.Column<int>(nullable: false),
                    ActiveFrom = table.Column<DateTimeOffset>(nullable: false),
                    ActiveTo = table.Column<DateTimeOffset>(nullable: false),
                    RankingId = table.Column<int>(nullable: false),
                    CustomerOrganisationId = table.Column<int>(nullable: false),
                    Motivation = table.Column<string>(maxLength: 1024, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: true),
                    UpdatedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quarantines", x => x.QuarantineId);
                    table.ForeignKey(
                        name: "FK_Quarantines_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quarantines_CustomerOrganisations_CustomerOrganisationId",
                        column: x => x.CustomerOrganisationId,
                        principalTable: "CustomerOrganisations",
                        principalColumn: "CustomerOrganisationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quarantines_Rankings_RankingId",
                        column: x => x.RankingId,
                        principalTable: "Rankings",
                        principalColumn: "RankingId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Quarantines_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuarantineHistoryEntries",
                columns: table => new
                {
                    QuarantineHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    QuarantineId = table.Column<int>(nullable: false),
                    LoggedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedById = table.Column<int>(nullable: true),
                    ActiveFrom = table.Column<DateTimeOffset>(nullable: false),
                    ActiveTo = table.Column<DateTimeOffset>(nullable: false),
                    Motivation = table.Column<string>(maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuarantineHistoryEntries", x => x.QuarantineHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_QuarantineHistoryEntries_Quarantines_QuarantineId",
                        column: x => x.QuarantineId,
                        principalTable: "Quarantines",
                        principalColumn: "QuarantineId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuarantineHistoryEntries_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineHistoryEntries_QuarantineId",
                table: "QuarantineHistoryEntries",
                column: "QuarantineId");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineHistoryEntries_UpdatedById",
                table: "QuarantineHistoryEntries",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Quarantines_CreatedBy",
                table: "Quarantines",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Quarantines_CustomerOrganisationId",
                table: "Quarantines",
                column: "CustomerOrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Quarantines_RankingId",
                table: "Quarantines",
                column: "RankingId");

            migrationBuilder.CreateIndex(
                name: "IX_Quarantines_UpdatedBy",
                table: "Quarantines",
                column: "UpdatedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuarantineHistoryEntries");

            migrationBuilder.DropTable(
                name: "Quarantines");
        }
    }
}
