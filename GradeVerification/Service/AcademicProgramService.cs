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

        // Method to retrieve all academic programs
        public List<AcademicProgram> GetPrograms()
        {
            return _dbContext.AcademicPrograms.ToList(); // Retrieve all programs
        }

        // New method to get ProgramId by program name
        public async Task<string?> GetProgramIdByNameAsync(string programName)
        {
            // Find the program by name using case-insensitive comparison
            var program = await _dbContext.AcademicPrograms
                .Where(p => p.ProgramCode.ToUpper() == programName.ToUpper()) // Convert both to upper case
                .FirstOrDefaultAsync();

            // Return the ProgramId if found, otherwise return null
            return program?.Id; // Assuming Id is the ProgramId
        }
    }
}