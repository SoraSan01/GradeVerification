using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Model
{
    public class YearLevel
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public string LevelName { get; set; }
        public bool IsDeleted { get; set; } = false;

        private string GenerateYearId()
        {
            return $"YR-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        public YearLevel()
        {
            Id = GenerateYearId();
        }
    }
}
