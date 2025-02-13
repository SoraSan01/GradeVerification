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
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateUserIdAsync()
        {
            var lastUser = await _context.Users
                .OrderByDescending(u => u.Id)
                .FirstOrDefaultAsync();

            if (lastUser == null)
            {
                return "USR-00001"; // First user
            }

            // Extract the numeric part of the last user ID and increment it
            int lastIdNumber = int.Parse(lastUser.Id.Split('-')[1]);
            return $"USR-{(lastIdNumber + 1):D5}"; // Generates USR-00002, USR-00003, etc.
        }

        public async Task<bool> AddUserAsync(User user)
        {
            user.Id = await GenerateUserIdAsync(); // Assign proper ID before saving
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
