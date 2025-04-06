using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToastNotifications.Messages.Error;

namespace GradeVerification.Model
{
    public class CompletionExam
    {
        [Key]
        public string ExamId { get; set; } = GenerateExamId();

        [Required]
        [ForeignKey("Student")]
        public string StudentId { get; set; }

        [Required]
        [ForeignKey("Grade")]
        public string GradeId { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")]
        public int Score { get; set; }

        [Required]
        public DateTime ExamDate { get; set; } = DateTime.Today;

        [Required]
        [ForeignKey("Professor")]
        public string ProfessorId { get; set; }

        [Required]
        public ExamStatus Status { get; set; } = ExamStatus.Pending;

        // Navigation Properties
        public virtual Student Student { get; set; }
        public virtual Grade Grade { get; set; }
        public virtual Professor Professor { get; set; }

        private static string GenerateExamId()
        {
            return "CEX-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
    }

    public enum ExamStatus
    {
        Pending,
        Completed,
        Approved,
        Rejected
    }
}
