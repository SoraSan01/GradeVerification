using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeVerification.Migrations
{
    /// <inheritdoc />
    public partial class AddedGradeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
    name: "Grade",
    columns: table => new
    {
        GradeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
        StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
        SubjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
        Score = table.Column<string>(type: "nvarchar(max)", nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_Grade", x => x.GradeId);
        table.ForeignKey(
            name: "FK_Grade_Students_StudentId",
            column: x => x.StudentId,
            principalTable: "Students",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade); // Cascade for Student
        table.ForeignKey(
            name: "FK_Grade_Subjects_SubjectId",
            column: x => x.SubjectId,
            principalTable: "Subjects",
            principalColumn: "SubjectId",
            onDelete: ReferentialAction.NoAction); // No Action for Subject
    });

            migrationBuilder.CreateIndex(
                name: "IX_Grade_StudentId",
                table: "Grade",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Grade_SubjectId",
                table: "Grade",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Grade");
        }
    }
}
