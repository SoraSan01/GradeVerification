using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Model
{
    public class Subject : INotifyPropertyChanged
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

        public string? Schedule { get; set; }
        public string? Professor { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation Property
        public virtual AcademicProgram AcademicProgram { get; set; }
        public virtual ICollection<Grade> Grades { get; set; }
        public string ProgramCode => AcademicProgram != null ? AcademicProgram.ProgramCode : string.Empty;

        [NotMapped] // This tells Entity Framework not to map this property to the database
        private bool _isSelected;

        [NotMapped] // Ensures it's only used in the UI, not stored in the database
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        // Generate a unique Subject ID with "SUB-" prefix
        private static string GenerateSubjectId()
        {
            return "SUB-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
