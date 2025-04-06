using GradeVerification.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Service
{
    public class AdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> DeleteStudentAsync(string studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
                return false;

            student.IsDeleted = true;
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreStudentAsync(string studentId)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Id == studentId && s.IsDeleted);
            if (student == null)
                return false;

            student.IsDeleted = false;
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
