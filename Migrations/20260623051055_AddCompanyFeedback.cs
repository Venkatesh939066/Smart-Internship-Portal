using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInternshipPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyFeedback",
                table: "Applications",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyFeedback",
                table: "Applications");
        }
    }
}
