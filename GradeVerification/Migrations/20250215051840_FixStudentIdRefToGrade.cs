using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeVerification.Migrations
{
    /// <inheritdoc />
    public partial class FixStudentIdRefToGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grade_Students_StudentRefId",
                table: "Grade");

            migrationBuilder.DropIndex(
                name: "IX_Grade_StudentRefId",
                table: "Grade");

            migrationBuilder.DropColumn(
                name: "StudentRefId",
                table: "Grade");

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "Grade",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Grade_StudentId",
                table: "Grade",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Grade_Students_StudentId",
                table: "Grade",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grade_Students_StudentId",
                table: "Grade");

            migrationBuilder.DropIndex(
                name: "IX_Grade_StudentId",
                table: "Grade");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Grade");

            migrationBuilder.AddColumn<string>(
                name: "StudentRefId",
                table: "Grade",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Grade_StudentRefId",
                table: "Grade",
                column: "StudentRefId");

            migrationBuilder.AddForeignKey(
                name: "FK_Grade_Students_StudentRefId",
                table: "Grade",
                column: "StudentRefId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
