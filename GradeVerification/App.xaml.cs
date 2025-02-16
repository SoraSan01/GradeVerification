using System;
using System.Windows;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Repository;
using GradeVerification.View.Admin;
using GradeVerification.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GradeVerification
{
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();

            // ✅ Register DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer("Server=DESKTOP-QRF1854\\SQLEXPRESS;Database=GradeVerification;Trusted_Connection=True;TrustServerCertificate=True;"));

            // ✅ Register repositories
            services.AddScoped<IRepository<Student>, Repository<Student>>();
            services.AddScoped<IRepository<Subject>, Repository<Subject>>();
            services.AddScoped<IRepository<AcademicProgram>, Repository<AcademicProgram>>();

            // ✅ Register ViewModels
            services.AddScoped<DashboardViewModel>();

            // ✅ Register MainWindow
            services.AddScoped<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // ✅ Resolve MainWindow using DI
                var mainWindow = _serviceProvider.GetService<MainWindow>();
                mainWindow?.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database connection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        public static T GetService<T>() => ((App)Current)._serviceProvider.GetService<T>();
    }
}
