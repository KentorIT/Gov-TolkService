using Microsoft.EntityFrameworkCore.Migrations;

namespace Tolk.BusinessLogic.Data.Migrations
{
    public partial class RequisitionsDbSet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requisition_AspNetUsers_CreatedBy",
                table: "Requisition");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisition_AspNetUsers_ImpersonatingCreatedBy",
                table: "Requisition");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisition_AspNetUsers_ImpersonatingProcessedBy",
                table: "Requisition");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisition_AspNetUsers_ProcessedBy",
                table: "Requisition");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisition_Requests_RequestId",
                table: "Requisition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Requisition",
                table: "Requisition");

            migrationBuilder.RenameTable(
                name: "Requisition",
                newName: "Requisitions");

            migrationBuilder.RenameIndex(
                name: "IX_Requisition_RequestId",
                table: "Requisitions",
                newName: "IX_Requisitions_RequestId");

            migrationBuilder.RenameIndex(
                name: "IX_Requisition_ProcessedBy",
                table: "Requisitions",
                newName: "IX_Requisitions_ProcessedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requisition_ImpersonatingProcessedBy",
                table: "Requisitions",
                newName: "IX_Requisitions_ImpersonatingProcessedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requisition_ImpersonatingCreatedBy",
                table: "Requisitions",
                newName: "IX_Requisitions_ImpersonatingCreatedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requisition_CreatedBy",
                table: "Requisitions",
                newName: "IX_Requisitions_CreatedBy");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Requisitions",
                table: "Requisitions",
                column: "RequisitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_AspNetUsers_CreatedBy",
                table: "Requisitions",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_AspNetUsers_ImpersonatingCreatedBy",
                table: "Requisitions",
                column: "ImpersonatingCreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_AspNetUsers_ImpersonatingProcessedBy",
                table: "Requisitions",
                column: "ImpersonatingProcessedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_AspNetUsers_ProcessedBy",
                table: "Requisitions",
                column: "ProcessedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requisitions_Requests_RequestId",
                table: "Requisitions",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_AspNetUsers_CreatedBy",
                table: "Requisitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_AspNetUsers_ImpersonatingCreatedBy",
                table: "Requisitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_AspNetUsers_ImpersonatingProcessedBy",
                table: "Requisitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_AspNetUsers_ProcessedBy",
                table: "Requisitions");

            migrationBuilder.DropForeignKey(
                name: "FK_Requisitions_Requests_RequestId",
                table: "Requisitions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Requisitions",
                table: "Requisitions");

            migrationBuilder.RenameTable(
                name: "Requisitions",
                newName: "Requisition");

            migrationBuilder.RenameIndex(
                name: "IX_Requisitions_RequestId",
                table: "Requisition",
                newName: "IX_Requisition_RequestId");

            migrationBuilder.RenameIndex(
                name: "IX_Requisitions_ProcessedBy",
                table: "Requisition",
                newName: "IX_Requisition_ProcessedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requisitions_ImpersonatingProcessedBy",
                table: "Requisition",
                newName: "IX_Requisition_ImpersonatingProcessedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requisitions_ImpersonatingCreatedBy",
                table: "Requisition",
                newName: "IX_Requisition_ImpersonatingCreatedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Requisitions_CreatedBy",
                table: "Requisition",
                newName: "IX_Requisition_CreatedBy");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Requisition",
                table: "Requisition",
                column: "RequisitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requisition_AspNetUsers_CreatedBy",
                table: "Requisition",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requisition_AspNetUsers_ImpersonatingCreatedBy",
                table: "Requisition",
                column: "ImpersonatingCreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requisition_AspNetUsers_ImpersonatingProcessedBy",
                table: "Requisition",
                column: "ImpersonatingProcessedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requisition_AspNetUsers_ProcessedBy",
                table: "Requisition",
                column: "ProcessedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requisition_Requests_RequestId",
                table: "Requisition",
                column: "RequestId",
                principalTable: "Requests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
