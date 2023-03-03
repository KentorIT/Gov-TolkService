using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class Add_CustomerSpecificPropertyTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerSpecificProperties",
                columns: table => new
                {
                    CustomerOrganisationId = table.Column<int>(type: "int", nullable: false),
                    PropertyType = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputPlaceholder = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    RemoteValidation = table.Column<bool>(type: "bit", nullable: false),
                    RegexPattern = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegexErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSpecificProperties", x => new { x.CustomerOrganisationId, x.PropertyType });
                    table.ForeignKey(
                        name: "FK_CustomerSpecificProperties_CustomerOrganisations_CustomerOrganisationId",
                        column: x => x.CustomerOrganisationId,
                        principalTable: "CustomerOrganisations",
                        principalColumn: "CustomerOrganisationId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSpecificProperties");
        }
    }
}
