using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Model
{
    public class Grade
    {
        [Key]
        public string GradeId { get; set; } = GenerateGradeId();

        [Required]
        [MaxLength(50)]
        [ForeignKey("Student")]
        public string StudentId { get; set; } // Foreign Key to Student's StudentId

        [Required]
        [ForeignKey("Subject")]
        public string SubjectId { get; set; } // Foreign Key to Subject

        public string? Score { get; set; } // Grade score (can be null initially)

        // Navigation Properties
        public virtual Student Student { get; set; }
        public virtual Subject Subject { get; set; }

        // Generate a unique Grade ID with "GRD-" prefix
        private static string GenerateGradeId()
        {
            return "GRD-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
    }
}
