using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_CustomerSpecificPropertyChangeHistoryEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "CustomerSpecificProperties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CustomerSpecificPropertyChangeHistoryEntries",
                columns: table => new
                {
                    CustomerSpecificPropertyHistoryEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputPlaceholder = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    RemoteValidation = table.Column<bool>(type: "bit", nullable: false),
                    RegexPattern = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegexErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    CustomerOrganisationId = table.Column<int>(type: "int", nullable: false),
                    PropertyType = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CustomerChangeLogEntryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSpecificPropertyChangeHistoryEntries", x => x.CustomerSpecificPropertyHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_CustomerSpecificPropertyChangeHistoryEntries_CustomerChangeLogEntries_CustomerChangeLogEntryId",
                        column: x => x.CustomerChangeLogEntryId,
                        principalTable: "CustomerChangeLogEntries",
                        principalColumn: "CustomerChangeLogEntryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerSpecificPropertyChangeHistoryEntries_CustomerOrganisations_CustomerOrganisationId",
                        column: x => x.CustomerOrganisationId,
                        principalTable: "CustomerOrganisations",
                        principalColumn: "CustomerOrganisationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSpecificPropertyChangeHistoryEntries_CustomerChangeLogEntryId",
                table: "CustomerSpecificPropertyChangeHistoryEntries",
                column: "CustomerChangeLogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSpecificPropertyChangeHistoryEntries_CustomerOrganisationId",
                table: "CustomerSpecificPropertyChangeHistoryEntries",
                column: "CustomerOrganisationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSpecificPropertyChangeHistoryEntries");

            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "CustomerSpecificProperties");
        }
    }
}
