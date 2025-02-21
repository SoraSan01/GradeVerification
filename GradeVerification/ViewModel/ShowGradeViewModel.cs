using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Helper;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

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

        public ICommand PrintCommand { get; }
        public ICommand CloseCommand { get; }

        public ShowGradeViewModel(Student student)
        {
            _student = student ?? throw new ArgumentNullException(nameof(student));
            _context = new ApplicationDbContext();
            CloseCommand = new RelayCommand(CloseWindow);
            PrintCommand = new RelayCommand(PrintWindow);
            CurrentDate = DateTime.Now.ToString("MMMM dd, yyyy");

            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight, offsetX: 10, offsetY: 10);
                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));
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

        private void PrintWindow(object parameter)
        {
            if (parameter is not UIElement printArea)
                return;

            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Temporarily hide buttons (assumes an extension method FindVisualChildren<Button>())
                var buttons = printArea.FindVisualChildren<Button>();
                foreach (var button in buttons)
                {
                    button.Visibility = Visibility.Collapsed;
                }

                printDialog.PrintVisual(printArea, "Printing Student Grades");

                foreach (var button in buttons)
                {
                    button.Visibility = Visibility.Visible;
                }
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
