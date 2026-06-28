using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalCollegeManagementSystem_AAU.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitClosedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClosedBy",
                table: "Visits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedDate",
                table: "Visits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClosed",
                table: "Visits",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedBy",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "ClosedDate",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "IsClosed",
                table: "Visits");
        }
    }
}
