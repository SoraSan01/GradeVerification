using GradeVerification.Data;
using GradeVerification.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GradeVerification.Service
{
    public class SubjectService
    {
        private readonly ApplicationDbContext _context;
        public SubjectService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<bool> SaveSubjectsAsync(ObservableCollection<Subject> subjects)
        {
            try
            {
                using (var dbContext = new ApplicationDbContext())
                {
                    // Fetch existing subjects to prevent duplicates
                    var existingSubjectCodes = await dbContext.Subjects
                        .Select(s => s.SubjectCode)
                        .ToListAsync(); // List<string>

                    // Filter out subjects that already exist in the database
                    var newSubjects = subjects
                        .Where(s => !existingSubjectCodes.Contains(s.SubjectCode))
                        .ToList(); // Convert to List<Subject>

                    if (newSubjects.Any())
                    {
                        await dbContext.Subjects.AddRangeAsync(newSubjects);
                        await dbContext.SaveChangesAsync();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving subjects: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Subject>> GetSubjectsByProgramAsync(string programId, string year, string semester)
        {
            return await _context.Subjects
                .Where(s => s.ProgramId == programId &&
                           s.Year == year &&
                           s.Semester == semester)
                .ToListAsync();
        }
    }
}
