using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Model
{
    public class Professor
    {
        [Key]
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; } = false;

        private string GenerateProfId()
        {
            return $"PROF-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        public Professor() { 
            Id = GenerateProfId();
        }

    }
}
