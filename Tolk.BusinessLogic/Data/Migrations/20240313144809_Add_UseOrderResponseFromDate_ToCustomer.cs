using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    /// <inheritdoc />
    public partial class Add_UseOrderResponseFromDate_ToCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UseOrderResponsesFromDate",
                table: "CustomerOrganisations",
                type: "datetime2",
                nullable: true);
            migrationBuilder.Sql(@"Exec(
'INSERT INTO [dbo].[CustomerSettings]([CustomerOrganisationId]
      ,[CustomerSettingType]
	  ,Value
      )
SELECT CustomerOrganisationId,5,0 FROM CustomerOrganisations
EXCEPT  
SELECT CustomerOrganisationId, [CustomerSettingType],0 FROM [CustomerSettings]')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseOrderResponsesFromDate",
                table: "CustomerOrganisations");
            migrationBuilder.Sql(@"Exec('DELETE FROM [dbo].[CustomerSettings] WHERE [CustomerSettingType] = 5')");
        }
    }
}
