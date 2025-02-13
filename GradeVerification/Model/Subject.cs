using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Model
{
    public class Subject
    {
        [Key]
        public string SubjectId { get; set; } = GenerateSubjectId();

        [Required]
        public string SubjectCode { get; set; }

        [Required]
        public string SubjectName { get; set; }

        [Required]
        public int Units { get; set; }

        [Required]
        public string Year { get; set; }

        [Required]
        [ForeignKey("AcademicProgram")]
        public string ProgramId { get; set; }  // Foreign Key

        [Required]
        public string Semester { get; set; }

        // Navigation Property
        public virtual AcademicProgram AcademicProgram { get; set; }

        public string ProgramCode => AcademicProgram != null ? AcademicProgram.ProgramCode : string.Empty;

        // Generate a unique Subject ID with "SUB-" prefix
        private static string GenerateSubjectId()
        {
            return "SUB-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
    }
}
