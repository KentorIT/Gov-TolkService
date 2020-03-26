using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddTableUserDefaultSettingOrderRequirement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDefaultSettingOrderRequirements",
                columns: table => new
                {
                    UserDefaultSettingOrderRequirementId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(nullable: false),
                    RequirementType = table.Column<int>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    IsRequired = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDefaultSettingOrderRequirements", x => x.UserDefaultSettingOrderRequirementId);
                    table.ForeignKey(
                        name: "FK_UserDefaultSettingOrderRequirements_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDefaultSettingOrderRequirements_UserId",
                table: "UserDefaultSettingOrderRequirements",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDefaultSettingOrderRequirements");
        }
    }
}
