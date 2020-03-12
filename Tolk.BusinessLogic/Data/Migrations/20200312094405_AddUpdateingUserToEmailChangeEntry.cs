using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddUpdateingUserToEmailChangeEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemporaryChangedEmailStoreEntries_AspNetUsers_UserId",
                table: "TemporaryChangedEmailStoreEntries");

            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingUpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries",
                nullable: true);

            // Add data from UserId
            migrationBuilder.Sql("Exec('Update TemporaryChangedEmailStoreEntries Set UpdatedByUserId = UserId')");

            // set nullable true
            migrationBuilder.AlterColumn<int>(
                name: "UpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryChangedEmailStoreEntries_ImpersonatingUpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries",
                column: "ImpersonatingUpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryChangedEmailStoreEntries_UpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemporaryChangedEmailStoreEntries_AspNetUsers_ImpersonatingUpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries",
                column: "ImpersonatingUpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TemporaryChangedEmailStoreEntries_AspNetUsers_UpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries",
                column: "UpdatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TemporaryChangedEmailStoreEntries_AspNetUsers_UserId",
                table: "TemporaryChangedEmailStoreEntries",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemporaryChangedEmailStoreEntries_AspNetUsers_ImpersonatingUpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_TemporaryChangedEmailStoreEntries_AspNetUsers_UpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_TemporaryChangedEmailStoreEntries_AspNetUsers_UserId",
                table: "TemporaryChangedEmailStoreEntries");

            migrationBuilder.DropIndex(
                name: "IX_TemporaryChangedEmailStoreEntries_ImpersonatingUpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries");

            migrationBuilder.DropIndex(
                name: "IX_TemporaryChangedEmailStoreEntries_UpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries");

            migrationBuilder.DropColumn(
                name: "ImpersonatingUpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "TemporaryChangedEmailStoreEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_TemporaryChangedEmailStoreEntries_AspNetUsers_UserId",
                table: "TemporaryChangedEmailStoreEntries",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
