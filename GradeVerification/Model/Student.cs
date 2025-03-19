using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeVerification.Model
{
    public class Student
    {
        [Key]
        [Required]
        public string Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string SchoolId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        // New MiddleName property (optional)
        [MaxLength(50)]
        public string MiddleName { get; set; }

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
        [MaxLength(20)]
        public string Year { get; set; } // e.g., "1st Year", "2nd Year" (student level)

        [Required]
        [MaxLength(20)]
        public string SchoolYear { get; set; } // e.g., "2022-2023" (academic year)

        [Required]
        [MaxLength(50)]
        public string Status { get; set; }

        [Required]
        [ForeignKey("AcademicProgram")]
        public string ProgramId { get; set; } // Foreign Key to AcademicProgram

        public virtual AcademicProgram AcademicProgram { get; set; } // Navigation Property
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

        // Updated FullName property to include MiddleName if present
        public string FullName => string.IsNullOrWhiteSpace(MiddleName)
            ? $"{LastName} {FirstName}"
            : $"{LastName} {FirstName} {MiddleName}";

        private string GenerateStudentId()
        {
            return $"STU-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        public Student()
        {
            Id = GenerateStudentId();
        }
    }
}
