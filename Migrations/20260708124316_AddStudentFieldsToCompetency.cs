using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalCollegeManagementSystem_AAU.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentFieldsToCompetency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudentUserID",
                table: "Competency",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TemplateCompetencyID",
                table: "Competency",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Competency",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Competency",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 1,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 2,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 3,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 4,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 5,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 6,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 7,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 8,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 9,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 10,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 11,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 12,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 13,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 14,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 15,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Competency",
                keyColumn: "CompetencyID",
                keyValue: 16,
                columns: new[] { "StudentUserID", "TemplateCompetencyID", "UpdatedBy", "UpdatedDate" },
                values: new object[] { null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Competency_StudentUserID_TemplateCompetencyID",
                table: "Competency",
                columns: new[] { "StudentUserID", "TemplateCompetencyID" },
                unique: true,
                filter: "[StudentUserID] IS NOT NULL AND [TemplateCompetencyID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Competency_TemplateCompetencyID",
                table: "Competency",
                column: "TemplateCompetencyID");

            migrationBuilder.AddForeignKey(
                name: "FK_Competency_Competency_TemplateCompetencyID",
                table: "Competency",
                column: "TemplateCompetencyID",
                principalTable: "Competency",
                principalColumn: "CompetencyID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Competency_Users_StudentUserID",
                table: "Competency",
                column: "StudentUserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Competency_Competency_TemplateCompetencyID",
                table: "Competency");

            migrationBuilder.DropForeignKey(
                name: "FK_Competency_Users_StudentUserID",
                table: "Competency");

            migrationBuilder.DropIndex(
                name: "IX_Competency_StudentUserID_TemplateCompetencyID",
                table: "Competency");

            migrationBuilder.DropIndex(
                name: "IX_Competency_TemplateCompetencyID",
                table: "Competency");

            migrationBuilder.DropColumn(
                name: "StudentUserID",
                table: "Competency");

            migrationBuilder.DropColumn(
                name: "TemplateCompetencyID",
                table: "Competency");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Competency");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Competency");
        }
    }
}
