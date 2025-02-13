using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeVerification.Migrations
{
    /// <inheritdoc />
    public partial class ChangeIntToStringSubjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the primary key constraint
            migrationBuilder.DropPrimaryKey(
                name: "PK_Subjects",
                table: "Subjects");

            // Drop the existing SubjectId column
            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Subjects");

            // Add a new SubjectId column with the string type
            migrationBuilder.AddColumn<string>(
                name: "SubjectId",
                table: "Subjects",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            // Recreate the primary key constraint on the new SubjectId column
            migrationBuilder.AddPrimaryKey(
                name: "PK_Subjects",
                table: "Subjects",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the primary key constraint
            migrationBuilder.DropPrimaryKey(
                name: "PK_Subjects",
                table: "Subjects");

            // Drop the new SubjectId column
            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Subjects");

            // Add the old SubjectId column back with the int type
            migrationBuilder.AddColumn<int>(
                name: "SubjectId",
                table: "Subjects",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            // Recreate the primary key constraint on the old SubjectId column
            migrationBuilder.AddPrimaryKey(
                name: "PK_Subjects",
                table: "Subjects",
                column: "SubjectId");
        }
    }
}