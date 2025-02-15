using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeVerification.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStudentIdToSchoolIdInStudentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StudentId",
                table: "Students",
                newName: "SchoolId");

            migrationBuilder.RenameIndex(
                name: "IX_Students_StudentId",
                table: "Students",
                newName: "IX_Students_SchoolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SchoolId",
                table: "Students",
                newName: "StudentId");

            migrationBuilder.RenameIndex(
                name: "IX_Students_SchoolId",
                table: "Students",
                newName: "IX_Students_StudentId");
        }
    }
}
