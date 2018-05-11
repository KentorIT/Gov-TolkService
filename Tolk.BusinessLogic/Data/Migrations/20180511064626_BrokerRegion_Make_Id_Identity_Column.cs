using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class BrokerRegion_Make_Id_Identity_Column : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_InterpreterBrokerRegion_BrokerRegions_BrokerRegionId", table: "InterpreterBrokerRegion");
            migrationBuilder.DropForeignKey(name: "FK_Rankings_BrokerRegions_BrokerRegionId", table: "Rankings");
            migrationBuilder.DropPrimaryKey(name: "PK_BrokerRegions", table: "BrokerRegions");
            migrationBuilder.DropForeignKey(name: "FK_BrokerRegions_Brokers_BrokerId", table: "BrokerRegions");
            migrationBuilder.DropColumn(name: "BrokerRegionId", table: "BrokerRegions");
            migrationBuilder.AddColumn<int>(
                name: "BrokerRegionId",
                table: "BrokerRegions",
                nullable: false)
                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);
            migrationBuilder.AddPrimaryKey(
                name: "PK_BrokerRegions",
                table: "BrokerRegions",
                column: "BrokerRegionId"
            );
            migrationBuilder.AddForeignKey(
                name: "FK_BrokerRegions_Brokers_BrokerId",
                table: "BrokerRegions",
                column: "BrokerId",
                principalTable: "Brokers",
                principalColumn: "BrokerId",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBrokerRegion_BrokerRegions_BrokerRegionId",
                table: "InterpreterBrokerRegion",
                column: "BrokerRegionId",
                principalTable: "BrokerRegions",
                principalColumn: "BrokerRegionId",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_Rankings_BrokerRegions_BrokerRegionId",
                table: "Rankings",
                column: "BrokerRegionId",
                principalTable: "BrokerRegions",
                principalColumn: "BrokerRegionId",
                onDelete: ReferentialAction.Cascade);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_InterpreterBrokerRegion_BrokerRegions_BrokerRegionId", table: "InterpreterBrokerRegion");
            migrationBuilder.DropForeignKey(name: "FK_Rankings_BrokerRegions_BrokerRegionId", table: "Rankings");
            migrationBuilder.DropPrimaryKey(name: "PK_BrokerRegions", table: "BrokerRegions");
            migrationBuilder.DropForeignKey(name: "FK_BrokerRegions_Brokers_BrokerId", table: "BrokerRegions");
            migrationBuilder.DropColumn(name: "BrokerRegionId", table: "BrokerRegions");
            migrationBuilder.AddColumn<int>(
                name: "BrokerRegionId",
                table: "BrokerRegions",
                nullable: false);
            migrationBuilder.AddPrimaryKey(
               name: "PK_BrokerRegions",
               table: "BrokerRegions",
               column: "BrokerRegionId"
           );
            migrationBuilder.AddForeignKey(
               name: "FK_BrokerRegions_Brokers_BrokerId",
               table: "BrokerRegions",
               column: "BrokerId",
               principalTable: "Brokers",
               principalColumn: "BrokerId",
               onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
               name: "FK_InterpreterBrokerRegion_BrokerRegions_BrokerRegionId",
               table: "InterpreterBrokerRegion",
               column: "BrokerRegionId",
               principalTable: "BrokerRegions",
               principalColumn: "BrokerRegionId",
               onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(
                name: "FK_Rankings_BrokerRegions_BrokerRegionId",
                table: "Rankings",
                column: "BrokerRegionId",
                principalTable: "BrokerRegions",
                principalColumn: "BrokerRegionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
