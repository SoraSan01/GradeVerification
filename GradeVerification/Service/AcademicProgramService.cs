using GradeVerification.Data;
using GradeVerification.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Service
{
    public class AcademicProgramService
    {
        private readonly ApplicationDbContext _dbContext;

        public AcademicProgramService()
        {
            _dbContext = new ApplicationDbContext(); // Assume Entity Framework is being used
        }

        public List<AcademicProgram> GetPrograms()
        {
            return _dbContext.AcademicPrograms.ToList(); // Retrieve all programs
        }

    }
}
