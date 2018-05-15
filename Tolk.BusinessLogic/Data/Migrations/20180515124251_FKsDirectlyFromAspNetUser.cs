using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class FKsDirectlyFromAspNetUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBroker");

            migrationBuilder.DropTable(
                name: "UserCustomerOrganisation");

            migrationBuilder.AddColumn<int>(
                name: "BrokerId",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerOrganisationId",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_BrokerId",
                table: "AspNetUsers",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CustomerOrganisationId",
                table: "AspNetUsers",
                column: "CustomerOrganisationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Brokers_BrokerId",
                table: "AspNetUsers",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "BrokerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_CustomerOrganisations_CustomerOrganisationId",
                table: "AspNetUsers",
                column: "CustomerOrganisationId",
                principalTable: "CustomerOrganisations",
                principalColumn: "CustomerOrganisationId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Brokers_BrokerId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_CustomerOrganisations_CustomerOrganisationId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_BrokerId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CustomerOrganisationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BrokerId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CustomerOrganisationId",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "UserBroker",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    BrokerId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBroker", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserBroker_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "BrokerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBroker_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCustomerOrganisation",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    CustomerOrganisationId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCustomerOrganisation", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserCustomerOrganisation_CustomerOrganisations_CustomerOrganisationId",
                        column: x => x.CustomerOrganisationId,
                        principalTable: "CustomerOrganisations",
                        principalColumn: "CustomerOrganisationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCustomerOrganisation_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBroker_BrokerId",
                table: "UserBroker",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCustomerOrganisation_CustomerOrganisationId",
                table: "UserCustomerOrganisation",
                column: "CustomerOrganisationId");
        }
    }
}
