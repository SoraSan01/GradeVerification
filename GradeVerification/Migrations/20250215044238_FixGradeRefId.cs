using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GradeVerification.Migrations
{
    /// <inheritdoc />
    public partial class FixGradeRefId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grade_Students_StudentId",
                table: "Grade");

            migrationBuilder.RenameColumn(
                name: "StudentId",
                table: "Grade",
                newName: "StudentRefId");

            migrationBuilder.RenameIndex(
                name: "IX_Grade_StudentId",
                table: "Grade",
                newName: "IX_Grade_StudentRefId");

            migrationBuilder.AddForeignKey(
                name: "FK_Grade_Students_StudentRefId",
                table: "Grade",
                column: "StudentRefId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grade_Students_StudentRefId",
                table: "Grade");

            migrationBuilder.RenameColumn(
                name: "StudentRefId",
                table: "Grade",
                newName: "StudentId");

            migrationBuilder.RenameIndex(
                name: "IX_Grade_StudentRefId",
                table: "Grade",
                newName: "IX_Grade_StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Grade_Students_StudentId",
                table: "Grade",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
