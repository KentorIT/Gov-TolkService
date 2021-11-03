using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddCustomerUnitTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerUnits",
                columns: table => new
                {
                    CustomerUnitId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 255, nullable: false),
                    Email = table.Column<string>(maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    CreatedBy = table.Column<int>(nullable: false),
                    CustomerOrganisationId = table.Column<int>(nullable: false),
                    ImpersonatingCreator = table.Column<int>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false),
                    InactivatedAt = table.Column<DateTimeOffset>(nullable: true),
                    InactivatedBy = table.Column<int>(nullable: true),
                    ImpersonatingInactivatedBy = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerUnits", x => x.CustomerUnitId);
                    table.ForeignKey(
                        name: "FK_CustomerUnits_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerUnits_CustomerOrganisations_CustomerOrganisationId",
                        column: x => x.CustomerOrganisationId,
                        principalTable: "CustomerOrganisations",
                        principalColumn: "CustomerOrganisationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerUnits_AspNetUsers_ImpersonatingCreator",
                        column: x => x.ImpersonatingCreator,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerUnits_AspNetUsers_ImpersonatingInactivatedBy",
                        column: x => x.ImpersonatingInactivatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerUnits_AspNetUsers_InactivatedBy",
                        column: x => x.InactivatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerUnitUsers",
                columns: table => new
                {
                    CustomerUnitId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    IsLocalAdmin = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerUnitUsers", x => new { x.CustomerUnitId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CustomerUnitUsers_CustomerUnits_CustomerUnitId",
                        column: x => x.CustomerUnitId,
                        principalTable: "CustomerUnits",
                        principalColumn: "CustomerUnitId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerUnitUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUnits_CreatedBy",
                table: "CustomerUnits",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUnits_CustomerOrganisationId",
                table: "CustomerUnits",
                column: "CustomerOrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUnits_ImpersonatingCreator",
                table: "CustomerUnits",
                column: "ImpersonatingCreator");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUnits_ImpersonatingInactivatedBy",
                table: "CustomerUnits",
                column: "ImpersonatingInactivatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUnits_InactivatedBy",
                table: "CustomerUnits",
                column: "InactivatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUnitUsers_UserId",
                table: "CustomerUnitUsers",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerUnitUsers");

            migrationBuilder.DropTable(
                name: "CustomerUnits");
        }
    }
}
