using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class UpdCustSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE CustomerSettings
            SET Value = ~Value
            WHERE CustomerSettingType = 3"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE CustomerSettings
            SET Value = ~Value
            WHERE CustomerSettingType = 3"
            );
        }
    }
}
