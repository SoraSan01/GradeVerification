using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GradeVerification.Model
{
    public class Student
    {
        [Key]
        [Required]
        public string Id { get; set; } // Primary Key

        [Required]
        [MaxLength(50)]
        public string StudentId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MaxLength(20)]
        public string Semester { get; set; } // e.g., "1st Semester", "2nd Semester"

        [Required]
        [MaxLength(10)]
        public string Year { get; set; } // e.g., "1st Year", "2nd Year"

        [Required]
        [MaxLength(50)]
        public string Status { get; set; }

        [Required]
        [ForeignKey("AcademicProgram")]
        public string ProgramId { get; set; } // Foreign Key to AcademicProgram

        public virtual AcademicProgram AcademicProgram { get; set; } // Navigation Property
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

        public string FullName => $"{FirstName} {LastName}";

        public Student()
        {
            Id = GenerateStudentId();
        }

        private string GenerateStudentId()
        {
            return $"STU-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

    }
}
