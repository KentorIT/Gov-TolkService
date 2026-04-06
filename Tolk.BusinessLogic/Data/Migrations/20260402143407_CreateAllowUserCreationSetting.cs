using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateAllowUserCreationSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //6=> CustomerSettingType.AllowUserSelfRegistration 
            migrationBuilder.Sql(@"
insert CustomerSettings
Select co.CustomerOrganisationId, 6, 1
from CustomerOrganisations co
Left Join CustomerSettings cs
On co.CustomerOrganisationId = cs.CustomerOrganisationId
and cs.CustomerSettingType = 6
Where cs.CustomerOrganisationId is null
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
Delete CustomerSettings
Where CustomerSettingType = 6
");
        }
    }
}
