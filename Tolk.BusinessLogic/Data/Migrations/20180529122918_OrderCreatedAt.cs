using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class OrderCreatedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn("CreatedDate", "Orders", "CreatedAt");

            migrationBuilder.AlterColumn<DateTimeOffset>("CreatedAt", "Orders");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>("CreatedAt", "Orders");

            migrationBuilder.RenameColumn("CreatedAt", "Orders", "CreatedDate");
        }
    }
}
