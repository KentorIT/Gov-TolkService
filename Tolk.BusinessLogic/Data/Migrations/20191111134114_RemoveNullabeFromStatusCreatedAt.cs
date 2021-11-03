using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RemoveNullabeFromStatusCreatedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ConfirmedBy",
                table: "RequestStatusConfirmation",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ConfirmedAt",
                table: "RequestStatusConfirmation",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ConfirmedBy",
                table: "OrderStatusConfirmation",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ConfirmedAt",
                table: "OrderStatusConfirmation",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ConfirmedBy",
                table: "RequestStatusConfirmation",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ConfirmedAt",
                table: "RequestStatusConfirmation",
                nullable: true,
                oldClrType: typeof(DateTimeOffset));

            migrationBuilder.AlterColumn<int>(
                name: "ConfirmedBy",
                table: "OrderStatusConfirmation",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ConfirmedAt",
                table: "OrderStatusConfirmation",
                nullable: true,
                oldClrType: typeof(DateTimeOffset));
        }
    }
}
