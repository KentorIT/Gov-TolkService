using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class orderwithreqs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    OrderNumber = table.Column<string>(maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    CreatedBy = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    CustomerOrganisationId = table.Column<int>(nullable: false),
                    RegionId = table.Column<int>(nullable: false),
                    CustomerReferenceNumber = table.Column<string>(maxLength: 100, nullable: true),
                    OtherContactPerson = table.Column<string>(maxLength: 255, nullable: true),
                    OtherContactPhone = table.Column<string>(maxLength: 50, nullable: true),
                    OtherContactEmail = table.Column<string>(maxLength: 255, nullable: true),
                    UnitName = table.Column<string>(maxLength: 100, nullable: true),
                    Street = table.Column<string>(maxLength: 100, nullable: true),
                    ZipCode = table.Column<string>(maxLength: 100, nullable: true),
                    City = table.Column<string>(maxLength: 100, nullable: true),
                    OtherAddressInformation = table.Column<string>(maxLength: 255, nullable: true),
                    LanguageId = table.Column<int>(nullable: false),
                    AssignentType = table.Column<int>(nullable: false),
                    RequiredInterpreterLocation = table.Column<int>(nullable: false),
                    RequestedInterpreterLocation = table.Column<int>(nullable: true),
                    RequiredCompetenceLevel = table.Column<int>(nullable: false),
                    RequestedCompetenceLevel = table.Column<int>(nullable: true),
                    StartDateTime = table.Column<DateTimeOffset>(nullable: false),
                    EndDateTime = table.Column<DateTimeOffset>(nullable: false),
                    AllowMoreThanTwoHoursTravelTime = table.Column<bool>(nullable: false),
                    Description = table.Column<string>(maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Orders_CustomerOrganisations_CustomerOrganisationId",
                        column: x => x.CustomerOrganisationId,
                        principalTable: "CustomerOrganisations",
                        principalColumn: "CustomerOrganisationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Languages_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Languages",
                        principalColumn: "LanguageId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "RegionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderRequirements",
                columns: table => new
                {
                    OrderRequirementId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RequirementType = table.Column<int>(nullable: false),
                    Description = table.Column<string>(maxLength: 100, nullable: true),
                    OrderId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRequirements", x => x.OrderRequirementId);
                    table.ForeignKey(
                        name: "FK_OrderRequirements_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderRequirements_OrderId",
                table: "OrderRequirements",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerOrganisationId",
                table: "Orders",
                column: "CustomerOrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RegionId",
                table: "Orders",
                column: "RegionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderRequirements");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
