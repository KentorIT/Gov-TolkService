using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class InterpreterForeignKeyRework : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Interpreter_InterpreterId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterBrokerRegion_BrokerRegions_BrokerRegionId",
                table: "InterpreterBrokerRegion");

            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterBrokerRegion_AspNetUsers_InterpreterId",
                table: "InterpreterBrokerRegion");

            migrationBuilder.DropForeignKey(
                name: "FK_Rankings_BrokerRegions_BrokerRegionId",
                table: "Rankings");

            migrationBuilder.DropIndex(
                name: "IX_Rankings_BrokerRegionId",
                table: "Rankings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterpreterBrokerRegion",
                table: "InterpreterBrokerRegion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BrokerRegions",
                table: "BrokerRegions");

            migrationBuilder.DropIndex(
                name: "IX_BrokerRegions_BrokerId",
                table: "BrokerRegions");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_InterpreterId",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Interpreter",
                table: "Interpreter");

            migrationBuilder.DropColumn(
                name: "BrokerRegionId",
                table: "BrokerRegions");

            migrationBuilder.RenameTable(
                name: "Interpreter",
                newName: "Interpreters");

            migrationBuilder.RenameColumn(
                name: "BrokerRegionId",
                table: "Rankings",
                newName: "RegionId");

            migrationBuilder.RenameColumn(
                name: "BrokerRegionId",
                table: "InterpreterBrokerRegion",
                newName: "RegionId");

            migrationBuilder.AddColumn<int>(
                name: "BrokerId",
                table: "Rankings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropIndex(
                name: "IX_InterpreterBrokerRegion_InterpreterId",
                table: "InterpreterBrokerRegion");

            migrationBuilder.AlterColumn<int>(
                name: "InterpreterId",
                table: "InterpreterBrokerRegion",
                nullable: false,
                oldClrType: typeof(string));

            migrationBuilder.CreateIndex(
                name: "IX_InterpreterBrokerRegion_InterpreterId",
                table: "InterpreterBrokerRegion",
                column: "InterpreterId");

            migrationBuilder.AddColumn<int>(
                name: "BrokerId",
                table: "InterpreterBrokerRegion",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterpreterBrokerRegion",
                table: "InterpreterBrokerRegion",
                columns: new[] { "BrokerId", "RegionId", "InterpreterId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BrokerRegions",
                table: "BrokerRegions",
                columns: new[] { "BrokerId", "RegionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Interpreters",
                table: "Interpreters",
                column: "InterpreterId");

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_BrokerId_RegionId",
                table: "Rankings",
                columns: new[] { "BrokerId", "RegionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_InterpreterId",
                table: "AspNetUsers",
                column: "InterpreterId",
                unique: true,
                filter: "[InterpreterId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Interpreters_InterpreterId",
                table: "AspNetUsers",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBrokerRegion_Interpreters_InterpreterId",
                table: "InterpreterBrokerRegion",
                column: "InterpreterId",
                principalTable: "Interpreters",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBrokerRegion_BrokerRegions_BrokerId_RegionId",
                table: "InterpreterBrokerRegion",
                columns: new[] { "BrokerId", "RegionId" },
                principalTable: "BrokerRegions",
                principalColumns: new[] { "BrokerId", "RegionId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Rankings_BrokerRegions_BrokerId_RegionId",
                table: "Rankings",
                columns: new[] { "BrokerId", "RegionId" },
                principalTable: "BrokerRegions",
                principalColumns: new[] { "BrokerId", "RegionId" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Interpreters_InterpreterId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterBrokerRegion_Interpreters_InterpreterId",
                table: "InterpreterBrokerRegion");

            migrationBuilder.DropForeignKey(
                name: "FK_InterpreterBrokerRegion_BrokerRegions_BrokerId_RegionId",
                table: "InterpreterBrokerRegion");

            migrationBuilder.DropForeignKey(
                name: "FK_Rankings_BrokerRegions_BrokerId_RegionId",
                table: "Rankings");

            migrationBuilder.DropIndex(
                name: "IX_Rankings_BrokerId_RegionId",
                table: "Rankings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterpreterBrokerRegion",
                table: "InterpreterBrokerRegion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BrokerRegions",
                table: "BrokerRegions");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_InterpreterId",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Interpreters",
                table: "Interpreters");

            migrationBuilder.DropColumn(
                name: "BrokerId",
                table: "Rankings");

            migrationBuilder.DropColumn(
                name: "BrokerId",
                table: "InterpreterBrokerRegion");

            migrationBuilder.RenameTable(
                name: "Interpreters",
                newName: "Interpreter");

            migrationBuilder.RenameColumn(
                name: "RegionId",
                table: "Rankings",
                newName: "BrokerRegionId");

            migrationBuilder.RenameColumn(
                name: "RegionId",
                table: "InterpreterBrokerRegion",
                newName: "BrokerRegionId");

            migrationBuilder.DropIndex(
                name: "IX_InterpreterBrokerRegion_InterpreterId",
                table: "InterpreterBrokerRegion");

            migrationBuilder.AlterColumn<string>(
                name: "InterpreterId",
                table: "InterpreterBrokerRegion",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.CreateIndex(
                name: "IX_InterpreterBrokerRegion_InterpreterId",
                table: "InterpreterBrokerRegion",
                column: "InterpreterId");

            migrationBuilder.AddColumn<int>(
                name: "BrokerRegionId",
                table: "BrokerRegions",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterpreterBrokerRegion",
                table: "InterpreterBrokerRegion",
                columns: new[] { "BrokerRegionId", "InterpreterId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BrokerRegions",
                table: "BrokerRegions",
                column: "BrokerRegionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Interpreter",
                table: "Interpreter",
                column: "InterpreterId");

            migrationBuilder.CreateIndex(
                name: "IX_Rankings_BrokerRegionId",
                table: "Rankings",
                column: "BrokerRegionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrokerRegions_BrokerId",
                table: "BrokerRegions",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_InterpreterId",
                table: "AspNetUsers",
                column: "InterpreterId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Interpreter_InterpreterId",
                table: "AspNetUsers",
                column: "InterpreterId",
                principalTable: "Interpreter",
                principalColumn: "InterpreterId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBrokerRegion_BrokerRegions_BrokerRegionId",
                table: "InterpreterBrokerRegion",
                column: "BrokerRegionId",
                principalTable: "BrokerRegions",
                principalColumn: "BrokerRegionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterpreterBrokerRegion_AspNetUsers_InterpreterId",
                table: "InterpreterBrokerRegion",
                column: "InterpreterId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
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
