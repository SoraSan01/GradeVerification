using System;
using System.Windows;
using GradeVerification.Data;
using GradeVerification.View.Admin;
using Microsoft.EntityFrameworkCore;

namespace GradeVerification
{
    public partial class App : Application
    {
        private ApplicationDbContext? _dbContext;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Create DbContextOptions
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlServer("Server=DESKTOP-QRF1854\\SQLEXPRESS;Database=GradeVerification;Trusted_Connection=True;TrustServerCertificate=True;");

                // Initialize the database context
                _dbContext = new ApplicationDbContext(optionsBuilder.Options);

                // Open the AdminWindow and pass the context
                var login = new MainWindow(_dbContext);
                login.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database connection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}