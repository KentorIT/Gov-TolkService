using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AuditLogTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetUserClaimHistoryEntries",
                columns: table => new
                {
                    AspNetUserClaimHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserAuditLogEntryId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true),
                    UserAuditLogEntry = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaimHistoryEntries", x => x.AspNetUserClaimHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaimHistoryEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserHistoryEntries",
                columns: table => new
                {
                    AspNetUserHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserAuditLogEntryId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    Email = table.Column<string>(maxLength: 255, nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    NameFirst = table.Column<string>(maxLength: 255, nullable: true),
                    NameFamily = table.Column<string>(maxLength: 255, nullable: true),
                    PhoneNumberCellphone = table.Column<string>(maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(nullable: false),
                    IsApiUser = table.Column<bool>(nullable: false),
                    UserAuditLogEntry = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserHistoryEntries", x => x.AspNetUserHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_AspNetUserHistoryEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoleHistoryEntries",
                columns: table => new
                {
                    AspNetUserRoleHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserAuditLogEntryId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    RoleId = table.Column<int>(nullable: false),
                    UserAuditLogEntry = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoleHistoryEntries", x => x.AspNetUserRoleHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoleHistoryEntries_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoleHistoryEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAuditLogEntries",
                columns: table => new
                {
                    UserAuditLogEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserChangeType = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    UpdatedByUserId = table.Column<int>(nullable: true),
                    LoggedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuditLogEntries", x => x.UserAuditLogEntryId);
                    table.ForeignKey(
                        name: "FK_UserAuditLogEntries_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAuditLogEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaimHistoryEntries_UserId",
                table: "AspNetUserClaimHistoryEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserHistoryEntries_UserId",
                table: "AspNetUserHistoryEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoleHistoryEntries_RoleId",
                table: "AspNetUserRoleHistoryEntries",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoleHistoryEntries_UserId",
                table: "AspNetUserRoleHistoryEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLogEntries_UpdatedByUserId",
                table: "UserAuditLogEntries",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuditLogEntries_UserId",
                table: "UserAuditLogEntries",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetUserClaimHistoryEntries");

            migrationBuilder.DropTable(
                name: "AspNetUserHistoryEntries");

            migrationBuilder.DropTable(
                name: "AspNetUserRoleHistoryEntries");

            migrationBuilder.DropTable(
                name: "UserAuditLogEntries");
        }
    }
}
