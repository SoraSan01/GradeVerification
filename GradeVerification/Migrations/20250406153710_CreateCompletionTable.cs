using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeVerification.Migrations
{
    /// <inheritdoc />
    public partial class CreateCompletionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompletionExams",
                columns: table => new
                {
                    ExamId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GradeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    ExamDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProfessorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletionExams", x => x.ExamId);
                    table.ForeignKey(
                        name: "FK_CompletionExams_Grade_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grade",
                        principalColumn: "GradeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompletionExams_Professors_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "Professors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompletionExams_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompletionExams");
        }
    }
}
