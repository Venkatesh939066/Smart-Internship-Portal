using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInternshipPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedJobFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "SavedJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SavedAt",
                table: "SavedJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "SavedJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "SavedJobs");

            migrationBuilder.DropColumn(
                name: "SavedAt",
                table: "SavedJobs");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "SavedJobs");
        }
    }
}
