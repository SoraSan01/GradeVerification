using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeVerification.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCompletionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompletionExams");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompletionExams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProfessorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExamDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExamType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinalGrade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GradeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PreviousGrade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Score = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletionExams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompletionExams_Grade_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grade",
                        principalColumn: "GradeId");
                    table.ForeignKey(
                        name: "FK_CompletionExams_Professors_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "Professors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompletionExams_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompletionExams_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompletionExams_GradeId",
                table: "CompletionExams",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletionExams_ProfessorId",
                table: "CompletionExams",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletionExams_StudentId",
                table: "CompletionExams",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CompletionExams_SubjectId",
                table: "CompletionExams",
                column: "SubjectId");
        }
    }
}
