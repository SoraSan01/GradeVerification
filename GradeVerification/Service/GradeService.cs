using GradeVerification.Data;
using GradeVerification.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Service
{
    public class GradeService
    {
        private readonly ApplicationDbContext _context;
        private readonly SubjectService _subjectService;

        public GradeService(ApplicationDbContext context, SubjectService subjectService)
        {
            _context = context;
            _subjectService = subjectService;
        }

        // Auto-assign subjects when creating scholar student
        public async Task AutoAssignSubjectsAsync(Student student)
        {
            if (student.Status.Equals("Scholar", StringComparison.OrdinalIgnoreCase))
            {
                var subjects = await _subjectService.GetSubjectsByProgramAsync(
                    student.ProgramId,
                    student.Year,
                    student.Semester
                );

                foreach (var subject in subjects)
                {
                    var grade = new Grade
                    {
                        StudentId = student.Id,
                        SubjectId = subject.SubjectId,
                        Score = null // Initialize with no grade
                    };

                    await _context.Grade.AddAsync(grade);
                }

                await _context.SaveChangesAsync();
            }
        }

        // Manually add subject to non-scholar student
        public async Task AddSubjectToStudentAsync(string studentId, string subjectId)
        {
            var grade = new Grade
            {
                StudentId = studentId,
                SubjectId = subjectId,
                Score = null
            };

            await _context.Grade.AddAsync(grade);
            await _context.SaveChangesAsync();
        }

        // Update grade for a subject
        public async Task UpdateGradeAsync(string gradeId, string score)
        {
            var grade = await _context.Grade.FindAsync(gradeId);
            if (grade != null)
            {
                grade.Score = score;
                await _context.SaveChangesAsync();
            }
        }
    }
}
