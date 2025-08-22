using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedasStajyerYonetimSistemi.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorApprovalWorkflowFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalHistories_AspNetUsers_ApproverId",
                table: "ApprovalHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Interns_AspNetUsers_ResponsiblePersonId",
                table: "Interns");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_ApproverId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemLogs_AspNetUsers_UserId",
                table: "SystemLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_AspNetUsers_ApproverId",
                table: "Timesheets");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Timesheets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SupervisorApprovalDate",
                table: "Timesheets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorId",
                table: "Timesheets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorName",
                table: "Timesheets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorNote",
                table: "Timesheets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "LeaveRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_ApplicationUserId",
                table: "Timesheets",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_SupervisorId",
                table: "Timesheets",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ApplicationUserId",
                table: "LeaveRequests",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalHistories_AspNetUsers_ApproverId",
                table: "ApprovalHistories",
                column: "ApproverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Interns_AspNetUsers_ResponsiblePersonId",
                table: "Interns",
                column: "ResponsiblePersonId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_ApplicationUserId",
                table: "LeaveRequests",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_ApproverId",
                table: "LeaveRequests",
                column: "ApproverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SystemLogs_AspNetUsers_UserId",
                table: "SystemLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_AspNetUsers_ApplicationUserId",
                table: "Timesheets",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_AspNetUsers_ApproverId",
                table: "Timesheets",
                column: "ApproverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_AspNetUsers_SupervisorId",
                table: "Timesheets",
                column: "SupervisorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalHistories_AspNetUsers_ApproverId",
                table: "ApprovalHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Interns_AspNetUsers_ResponsiblePersonId",
                table: "Interns");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_ApplicationUserId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_ApproverId",
                table: "LeaveRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SystemLogs_AspNetUsers_UserId",
                table: "SystemLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_AspNetUsers_ApplicationUserId",
                table: "Timesheets");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_AspNetUsers_ApproverId",
                table: "Timesheets");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_AspNetUsers_SupervisorId",
                table: "Timesheets");

            migrationBuilder.DropIndex(
                name: "IX_Timesheets_ApplicationUserId",
                table: "Timesheets");

            migrationBuilder.DropIndex(
                name: "IX_Timesheets_SupervisorId",
                table: "Timesheets");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_ApplicationUserId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "SupervisorApprovalDate",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "SupervisorName",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "SupervisorNote",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "LeaveRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalHistories_AspNetUsers_ApproverId",
                table: "ApprovalHistories",
                column: "ApproverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Interns_AspNetUsers_ResponsiblePersonId",
                table: "Interns",
                column: "ResponsiblePersonId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_ApproverId",
                table: "LeaveRequests",
                column: "ApproverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SystemLogs_AspNetUsers_UserId",
                table: "SystemLogs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_AspNetUsers_ApproverId",
                table: "Timesheets",
                column: "ApproverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
