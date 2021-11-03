using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddColumnLatestAnswerTimeForCustomerToRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LatestAnswerTimeForCustomer",
                table: "Requests",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LatestAnswerTimeForCustomer",
                table: "RequestGroups",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatestAnswerTimeForCustomer",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "LatestAnswerTimeForCustomer",
                table: "RequestGroups");
        }
    }
}
