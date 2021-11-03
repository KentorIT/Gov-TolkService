using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddTableSystemMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemMessages",
                columns: table => new
                {
                    SystemMessageId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    CreatedBy = table.Column<int>(nullable: false),
                    ImpersonatingCreator = table.Column<int>(nullable: true),
                    SystemMessageHeader = table.Column<string>(maxLength: 255, nullable: true),
                    SystemMessageText = table.Column<string>(maxLength: 2000, nullable: true),
                    ActiveFrom = table.Column<DateTimeOffset>(nullable: false),
                    ActiveTo = table.Column<DateTimeOffset>(nullable: false),
                    SystemMessageType = table.Column<int>(nullable: false),
                    SystemMessageUserTypeGroup = table.Column<int>(nullable: false),
                    LastUpdatedBy = table.Column<int>(nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemMessages", x => x.SystemMessageId);
                    table.ForeignKey(
                        name: "FK_SystemMessages_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemMessages_AspNetUsers_ImpersonatingCreator",
                        column: x => x.ImpersonatingCreator,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemMessages_AspNetUsers_LastUpdatedBy",
                        column: x => x.LastUpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemMessages_CreatedBy",
                table: "SystemMessages",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SystemMessages_ImpersonatingCreator",
                table: "SystemMessages",
                column: "ImpersonatingCreator");

            migrationBuilder.CreateIndex(
                name: "IX_SystemMessages_LastUpdatedBy",
                table: "SystemMessages",
                column: "LastUpdatedBy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemMessages");
        }
    }
}
