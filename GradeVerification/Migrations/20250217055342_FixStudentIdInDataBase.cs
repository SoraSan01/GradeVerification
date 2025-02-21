using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeVerification.Migrations
{
    /// <inheritdoc />
    public partial class FixStudentIdInDataBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_SchoolId",
                table: "Students");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Students_SchoolId",
                table: "Students",
                column: "SchoolId",
                unique: true);
        }
    }
}
