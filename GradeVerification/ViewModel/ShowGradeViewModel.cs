using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Helper;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;
using System.Windows.Shapes;

using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Printing;

namespace GradeVerification.ViewModel
{
    public class ShowGradeViewModel : INotifyPropertyChanged
    {
        private readonly Student _student;
        private readonly ApplicationDbContext _context;
        private Notifier _notifier;
        private bool _isLoading;

        public ObservableCollection<Grade> Grades { get; set; } = new ObservableCollection<Grade>();

        private string _currentDate;
        public string CurrentDate
        {
            get => _currentDate;
            set { _currentDate = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // Expose the student to the view for binding
        public Student Student => _student;

        public ICommand CloseCommand { get; }

        public ShowGradeViewModel(Student student)
        {
            _student = student ?? throw new ArgumentNullException(nameof(student));
            _context = new ApplicationDbContext();
            CloseCommand = new RelayCommand(CloseWindow);
            CurrentDate = DateTime.Now.ToString("MMMM dd, yyyy");

            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(1.5),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(100));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            LoadGrades();
        }

        private async void LoadGrades()
        {
            try
            {
                IsLoading = true;
                var grades = await _context.Grade
                    .Include(g => g.Subject)
                    .Where(g => g.StudentId == _student.Id)
                    .ToListAsync();

                Grades.Clear();
                foreach (var grade in grades)
                {
                    Grades.Add(grade);
                }
                if (!Grades.Any())
                {
                    _notifier.ShowError("No grade records found for this student.");
                }
            }
            catch (Exception ex)
            {
                _notifier.ShowError($"Error loading grades: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CloseWindow(object parameter)
        {
            Application.Current.Windows.OfType<ShowGradeWindow>().FirstOrDefault()?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
