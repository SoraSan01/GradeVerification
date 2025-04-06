using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Model
{
    public class SchoolYear
    {
        [Key]
        public string Id { get; set; }
        public string SchoolYears { get; set; }
        public bool IsDeleted { get; set; } = false;

        private string GerateSchooYearId()
        {
            return $"SY-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        public SchoolYear() {
            Id = GerateSchooYearId();
        }
    }
}
