using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeVerification.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessorAndSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Professor",
                table: "Grade");

            migrationBuilder.DropColumn(
                name: "Schedule",
                table: "Grade");

            migrationBuilder.AddColumn<string>(
                name: "Professor",
                table: "Subjects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Schedule",
                table: "Subjects",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Professor",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "Schedule",
                table: "Subjects");

            migrationBuilder.AddColumn<string>(
                name: "Professor",
                table: "Grade",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Schedule",
                table: "Grade",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
