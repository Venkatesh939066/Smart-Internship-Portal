using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInternshipPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddEducationPercentageAndCertifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Certifications",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EducationPercentage",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Certifications",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EducationPercentage",
                table: "AspNetUsers");
        }
    }
}
