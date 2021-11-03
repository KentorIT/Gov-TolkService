using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class AddOrderChangeHistoryTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
              name: "OrderChangeLogEntries",
              columns: table => new
              {
                  OrderChangeLogEntryId = table.Column<int>(nullable: false)
                      .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                  OrderChangeLogType = table.Column<int>(nullable: false),
                  OrderId = table.Column<int>(nullable: false),
                  UpdatedByUserId = table.Column<int>(nullable: true),
                  UpdatedByImpersonatorId = table.Column<int>(nullable: true),
                  LoggedAt = table.Column<DateTimeOffset>(nullable: false)
              },
              constraints: table =>
              {
                  table.PrimaryKey("PK_OrderChangeLogEntries", x => x.OrderChangeLogEntryId);
                  table.ForeignKey(
                      name: "FK_OrderChangeLogEntries_Orders_OrderId",
                      column: x => x.OrderId,
                      principalTable: "Orders",
                      principalColumn: "OrderId",
                      onDelete: ReferentialAction.Cascade);
                  table.ForeignKey(
                      name: "FK_OrderChangeLogEntries_AspNetUsers_UpdatedByImpersonatorId",
                      column: x => x.UpdatedByImpersonatorId,
                      principalTable: "AspNetUsers",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
                  table.ForeignKey(
                      name: "FK_OrderChangeLogEntries_AspNetUsers_UpdatedByUserId",
                      column: x => x.UpdatedByUserId,
                      principalTable: "AspNetUsers",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
              });

            migrationBuilder.CreateTable(
                name: "OrderHistoryEntries",
                columns: table => new
                {
                    OrderHistoryEntryId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    OrderChangeLogEntryId = table.Column<int>(nullable: false),
                    ChangeOrderType = table.Column<int>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderHistoryEntries", x => x.OrderHistoryEntryId);
                    table.ForeignKey(
                        name: "FK_OrderHistoryEntries_OrderChangeLogEntries_OrderChangeLogEntryId",
                        column: x => x.OrderChangeLogEntryId,
                        principalTable: "OrderChangeLogEntries",
                        principalColumn: "OrderChangeLogEntryId",
                        onDelete: ReferentialAction.Cascade);
                });



            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeLogEntries_OrderId",
                table: "OrderChangeLogEntries",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeLogEntries_UpdatedByImpersonatorId",
                table: "OrderChangeLogEntries",
                column: "UpdatedByImpersonatorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderChangeLogEntries_UpdatedByUserId",
                table: "OrderChangeLogEntries",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderHistoryEntries_OrderChangeLogEntryId",
                table: "OrderHistoryEntries",
                column: "OrderChangeLogEntryId");

            migrationBuilder.DropForeignKey(
             name: "FK_OrderContactPersonHistory_Orders_OrderId",
             table: "OrderContactPersonHistory");

            migrationBuilder.DropIndex(
               name: "IX_OrderContactPersonHistory_OrderId",
               table: "OrderContactPersonHistory");

            migrationBuilder.Sql(@"
            Exec('
            INSERT INTO OrderChangeLogEntries(OrderChangeLogType, OrderId, UpdatedByUserId, LoggedAt, UpdatedByImpersonatorId)
            SELECT 2, OrderId, ChangedBy, ChangedAt, ImpersonatingChangeUserId 
            FROM OrderContactPersonHistory

            UPDATE OrderContactPersonHistory SET OrderId = oc.OrderChangeLogEntryId
            FROM OrderContactPersonHistory ocp
            JOIN OrderChangeLogEntries oc
                ON oc.LoggedAt = ocp.ChangedAt AND oc.OrderId = ocp.OrderId')"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_OrderContactPersonHistory_AspNetUsers_ChangedBy",
                table: "OrderContactPersonHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderContactPersonHistory_AspNetUsers_ImpersonatingChangeUserId",
                table: "OrderContactPersonHistory");

            migrationBuilder.DropIndex(
                name: "IX_OrderContactPersonHistory_ChangedBy",
                table: "OrderContactPersonHistory");

            migrationBuilder.DropIndex(
                name: "IX_OrderContactPersonHistory_ImpersonatingChangeUserId",
                table: "OrderContactPersonHistory");

            migrationBuilder.DropColumn(
                name: "ChangedAt",
                table: "OrderContactPersonHistory");

            migrationBuilder.DropColumn(
                name: "ChangedBy",
                table: "OrderContactPersonHistory");

            migrationBuilder.DropColumn(
                name: "ImpersonatingChangeUserId",
                table: "OrderContactPersonHistory");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "OrderContactPersonHistory",
                newName: "OrderChangeLogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderContactPersonHistory_OrderChangeLogEntryId",
                table: "OrderContactPersonHistory",
                column: "OrderChangeLogEntryId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderContactPersonHistory_OrderChangeLogEntries_OrderChangeLogEntryId",
                table: "OrderContactPersonHistory",
                column: "OrderChangeLogEntryId",
                principalTable: "OrderChangeLogEntries",
                principalColumn: "OrderChangeLogEntryId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ChangedAt",
            table: "OrderContactPersonHistory",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "ChangedBy",
                table: "OrderContactPersonHistory",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingChangeUserId",
                table: "OrderContactPersonHistory",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderContactPersonHistory_ChangedBy",
                table: "OrderContactPersonHistory",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrderContactPersonHistory_ImpersonatingChangeUserId",
                table: "OrderContactPersonHistory",
                column: "ImpersonatingChangeUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderContactPersonHistory_AspNetUsers_ChangedBy",
                table: "OrderContactPersonHistory",
                column: "ChangedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderContactPersonHistory_AspNetUsers_ImpersonatingChangeUserId",
                table: "OrderContactPersonHistory",
                column: "ImpersonatingChangeUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropForeignKey(
                name: "FK_OrderContactPersonHistory_OrderChangeLogEntries_OrderChangeLogEntryId",
                table: "OrderContactPersonHistory");

            migrationBuilder.DropIndex(
                name: "IX_OrderContactPersonHistory_OrderChangeLogEntryId",
                table: "OrderContactPersonHistory");

            migrationBuilder.Sql(@"
            UPDATE OrderContactPersonHistory SET 
            ChangedAt = oc.LoggedAt, 
            ChangedBy = oc.UpdatedByUserId, 
            ImpersonatingChangeUserId = oc.UpdatedByImpersonatorId,
            OrderChangeLogEntryId = oc.OrderId
            FROM OrderContactPersonHistory ocp
            JOIN OrderChangeLogEntries oc
                ON oc.OrderChangeLogEntryId = ocp.OrderChangeLogEntryId"
            );

            migrationBuilder.DropTable(
                name: "OrderHistoryEntries");

            migrationBuilder.DropTable(
                name: "OrderChangeLogEntries");

            migrationBuilder.RenameColumn(
                name: "OrderChangeLogEntryId",
                table: "OrderContactPersonHistory",
                newName: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderContactPersonHistory_Orders_OrderId",
                table: "OrderContactPersonHistory",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(
               name: "IX_OrderContactPersonHistory_OrderId",
               table: "OrderContactPersonHistory",
               column: "OrderId");

        }
    }
}
