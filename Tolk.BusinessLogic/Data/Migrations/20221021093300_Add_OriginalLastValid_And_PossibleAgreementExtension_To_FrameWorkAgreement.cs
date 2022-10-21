using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class Add_OriginalLastValid_And_PossibleAgreementExtension_To_FrameWorkAgreement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OriginalLastValidDate",
                table: "FrameworkAgreements",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "PossibleAgreementExtensionsInMonths",
                table: "FrameworkAgreements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalLastValidDate",
                table: "FrameworkAgreements");

            migrationBuilder.DropColumn(
                name: "PossibleAgreementExtensionsInMonths",
                table: "FrameworkAgreements");
        }
    }
}
