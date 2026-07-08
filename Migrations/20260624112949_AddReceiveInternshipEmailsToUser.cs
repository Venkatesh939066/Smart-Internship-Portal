using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInternshipPortal.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiveInternshipEmailsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReceiveInternshipEmails",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiveInternshipEmails",
                table: "AspNetUsers");
        }
    }
}
