using GradeVerification.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<AcademicProgram> AcademicPrograms { get; set; }
        public DbSet<Subject> Subjects { get; set; }

        public ApplicationDbContext() { }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=DESKTOP-QRF1854\\SQLEXPRESS;Database=GradeVerification;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subject>()
                .HasOne(s => s.AcademicProgram)
                .WithMany(p => p.Subjects)
                .HasForeignKey(s => s.ProgramId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete if needed
        }
    }

}
