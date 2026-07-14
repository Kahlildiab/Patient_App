using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalCollegeManagementSystem_AAU.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentNoteApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NoteType",
                table: "Notes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Notes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Notes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "Notes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "Notes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByRole",
                table: "Notes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Notes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VisitId",
                table: "Notes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notes_VisitId_ApprovalStatus",
                table: "Notes",
                columns: new[] { "VisitId", "ApprovalStatus" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Visits_VisitId",
                table: "Notes",
                column: "VisitId",
                principalTable: "Visits",
                principalColumn: "VisitID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Visits_VisitId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_VisitId_ApprovalStatus",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "CreatedByRole",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "VisitId",
                table: "Notes");

            migrationBuilder.AlterColumn<string>(
                name: "NoteType",
                table: "Notes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Notes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
