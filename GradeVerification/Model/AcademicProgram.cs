using System;
using System.ComponentModel.DataAnnotations;

namespace GradeVerification.Model
{
    public class AcademicProgram
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public string ProgramCode { get; set; }

        [Required]
        public string ProgramName { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation Property (One-to-Many)
        public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();

        public void GenerateNewId(string lastId)
        {
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            int newCounter = 1;

            if (!string.IsNullOrEmpty(lastId) && lastId.StartsWith($"PRG-{datePart}"))
            {
                string lastCounterStr = lastId.Substring(lastId.LastIndexOf('-') + 1);
                if (int.TryParse(lastCounterStr, out int lastCounter))
                {
                    newCounter = lastCounter + 1;
                }
            }

            Id = $"PRG-{datePart}-{newCounter:D3}";
        }
    }
}
