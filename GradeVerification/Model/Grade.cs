using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Globalization;

namespace GradeVerification.Model
{
    public class Grade
    {
        private string? _score;
        private bool _hasCompletionExam;

        [Key]
        public string GradeId { get; set; } = GenerateGradeId();

        [Required]
        [ForeignKey("Student")]
        public string StudentId { get; set; }

        [Required]
        [ForeignKey("Subject")]
        public string SubjectId { get; set; }

        [Required]
        public string ProfessorName { get; set; }

        public string? Score
        {
            get => _score;
            set
            {
                _score = value;
                UpdateCompletionEligibility();
            }
        }

        public string? CompletionGrade { get; set; }

        // Determines if eligible for a completion exam
        public bool CompletionEligible { get; private set; } = false;
        public bool IsDeleted { get; set; } = false;

        // Indicates if a completion exam exists
        public bool HasCompletionExam
        {
            get => _hasCompletionExam;
            set
            {
                _hasCompletionExam = value;
                UpdateCompletionEligibility();
            }
        }

        // List of non-passing grades
        private static readonly HashSet<string> NonPassingScores = new HashSet<string>
        {
            "INC", "N/A", "NGS", "NN", "-", "DROP"
        };

        [NotMapped]
        public decimal? GradeAsNumber
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Score)) return null;
                if (decimal.TryParse(Score, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal numericGrade))
                    return numericGrade;
                return null;
            }
        }

        [NotMapped]
        public bool IsGradeLow => (Score != null && NonPassingScores.Contains(Score.ToUpper())); // Non-numeric failing scores

        // Navigation Properties
        public virtual Student Student { get; set; }
        public virtual Subject Subject { get; set; }
        private void UpdateCompletionEligibility()
        {
            // Eligible if the grade is low and no completion exam exists
            CompletionEligible = IsGradeLow && !HasCompletionExam;
        }

        private static string GenerateGradeId()
        {
            return "GRD-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
    }

}
