using GradeVerification.Data;
using GradeVerification.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Service
{
    public class SubjectService
    {
        public async Task<bool> SaveSubjectsAsync(ObservableCollection<Subject> subjects)
        {
            try
            {
                // Database insert logic here, for example:
                using (var dbContext = new ApplicationDbContext())
                {
                    foreach (var subject in subjects)
                    {
                        dbContext.Subjects.Add(subject);
                    }
                    await dbContext.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Log the exception or display an error message
                Console.WriteLine($"Error saving subjects: {ex.Message}");
                return false;
            }
        }

    }
}