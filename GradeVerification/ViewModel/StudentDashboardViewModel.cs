using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class StudentDashboardViewModel : INotifyPropertyChanged
    {
        private readonly Notifier _notifier;
        public ObservableCollection<Student> Students { get; set; } = new ObservableCollection<Student>();
        private ObservableCollection<Student> _allStudents = new ObservableCollection<Student>();

        public ObservableCollection<string> Semesters { get; set; } = new ObservableCollection<string> { "First Semester", "Second Semester", "Summer" };
        public ObservableCollection<string> Years { get; set; } = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
        public ObservableCollection<string> Programs { get; set; } = new ObservableCollection<string>();

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterStudents(); }
        }

        private string _selectedSemester;
        public string SelectedSemester
        {
            get => _selectedSemester;
            set { _selectedSemester = value; OnPropertyChanged(); FilterStudents(); }
        }

        private string _selectedYear;
        public string SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(); FilterStudents(); }
        }

        private string _selectedProgram;
        public string SelectedProgram
        {
            get => _selectedProgram;
            set { _selectedProgram = value; OnPropertyChanged(); FilterStudents(); }
        }

        // Commands for various actions.
        public ICommand AddStudentCommand { get; }
        public ICommand EditStudentCommand { get; }
        public ICommand DeleteStudentCommand { get; }
        public ICommand ShowGradeCommand { get; }
        public ICommand UploadStudentCommand { get; }

        public StudentDashboardViewModel()
        {
            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight, offsetX: 10, offsetY: 10);
                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));
                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            AddStudentCommand = new RelayCommand(AddStudent);
            EditStudentCommand = new RelayCommand(EditStudent, CanModifyStudent);
            DeleteStudentCommand = new RelayCommand(async param => await DeleteStudent(param), CanModifyStudent);
            ShowGradeCommand = new RelayCommand(ShowGrade, CanModifyStudent);
            UploadStudentCommand = new RelayCommand(UploadWindow);

            // Load students initially.
            LoadStudentsAsync();
        }

        /// <summary>
        /// Loads all students asynchronously and updates the master and filtered collections.
        /// </summary>
        private async void LoadStudentsAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var studentList = await context.Students.Include(s => s.AcademicProgram).ToListAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _allStudents.Clear();
                        Students.Clear();
                        Programs.Clear();

                        foreach (var student in studentList)
                        {
                            _allStudents.Add(student);
                            Students.Add(student);
                            if (!Programs.Contains(student.AcademicProgram.ProgramCode))
                            {
                                Programs.Add(student.AcademicProgram.ProgramCode);
                            }
                        }
                        // Optionally, raise a property changed for an IsEmpty property.
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading students: {ex.Message}");
                _notifier.ShowError("Error loading students.");
            }
        }

        /// <summary>
        /// Filters the student list based on the search text and filter selections.
        /// </summary>
        private void FilterStudents()
        {
            Students.Clear();
            foreach (var student in _allStudents)
            {
                bool matchesSearch = string.IsNullOrWhiteSpace(SearchText) ||
                                     student.FullName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     student.SchoolId.Contains(SearchText);
                bool matchesSemester = string.IsNullOrWhiteSpace(SelectedSemester) || student.Semester.Equals(SelectedSemester, StringComparison.OrdinalIgnoreCase);
                bool matchesYear = string.IsNullOrWhiteSpace(SelectedYear) || student.Year.Equals(SelectedYear, StringComparison.OrdinalIgnoreCase);
                bool matchesProgram = string.IsNullOrWhiteSpace(SelectedProgram) || student.AcademicProgram.ProgramCode.Equals(SelectedProgram, StringComparison.OrdinalIgnoreCase);

                if (matchesSearch && matchesSemester && matchesYear && matchesProgram)
                {
                    Students.Add(student);
                }
            }

            // Optionally, you could show a notification if Students.Count == 0.
        }

        private void AddStudent(object parameter)
        {
            var addStudentWindow = new AddStudent
            {
                DataContext = new AddStudentViewModel(LoadStudentsAsync)
            };

            if (addStudentWindow.ShowDialog() == true)
            {
                LoadStudentsAsync();
            }
        }

        private void EditStudent(object parameter)
        {
            if (parameter is Student studentToEdit)
            {
                var editWindow = new EditStudent(); // Create the edit window.
                editWindow.DataContext = new EditStudentViewModel(studentToEdit, editWindow, LoadStudentsAsync);
                editWindow.Show();
            }
        }

        private async Task DeleteStudent(object parameter)
        {
            if (parameter is Student studentToDelete)
            {
                var result = MessageBox.Show($"Are you sure you want to delete {studentToDelete.FullName}?",
                    "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new ApplicationDbContext())
                        {
                            context.Students.Remove(studentToDelete);
                            await context.SaveChangesAsync();
                            LoadStudentsAsync();
                        }
                        ShowSuccessNotification("Student deleted successfully!");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting student: {ex.Message}");
                        ShowErrorNotification("Error deleting student.");
                    }
                }
            }
        }

        private bool CanModifyStudent(object parameter) => parameter is Student;

        private void ShowGrade(object parameter)
        {
            if (parameter is Student student)
            {
                var showGradeWindow = new ShowGradeWindow
                {
                    DataContext = new ShowGradeViewModel(student)
                };
                showGradeWindow.ShowDialog();
            }
        }

        private void UploadWindow(object parameter)
        {
            var uploadStudent = new UploadStudent();
            uploadStudent.DataContext = new UploadStudentViewModel();
            uploadStudent.Show();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ShowSuccessNotification(string message)
        {
            _notifier.ShowSuccess(message);
        }

        private void ShowErrorNotification(string message)
        {
            _notifier.ShowError(message);
        }
    }
}
