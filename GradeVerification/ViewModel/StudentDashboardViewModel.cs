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
using System.Windows.Threading;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class StudentDashboardViewModel : INotifyPropertyChanged
    {
        private readonly Notifier _notifier;
        private readonly DispatcherTimer _filterTimer;

        public ObservableCollection<Student> Students { get; set; } = new ObservableCollection<Student>();
        private ObservableCollection<Student> _allStudents = new ObservableCollection<Student>();

        public ObservableCollection<string> Semesters { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Years { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Programs { get; set; } = new ObservableCollection<string>();
        // New SchoolYears collection.
        public ObservableCollection<string> SchoolYears { get; set; } = new ObservableCollection<string>();

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ResetFilterTimer(); }
        }

        private string _selectedSemester;
        public string SelectedSemester
        {
            get => _selectedSemester;
            set { _selectedSemester = value; OnPropertyChanged(); ResetFilterTimer(); }
        }

        private string _selectedYear;
        public string SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(); ResetFilterTimer(); }
        }

        // New SelectedSchoolYear property.
        private string _selectedSchoolYear;
        public string SelectedSchoolYear
        {
            get => _selectedSchoolYear;
            set { _selectedSchoolYear = value; OnPropertyChanged(); ResetFilterTimer(); }
        }

        private string _selectedProgram;
        public string SelectedProgram
        {
            get => _selectedProgram;
            set { _selectedProgram = value; OnPropertyChanged(); ResetFilterTimer(); }
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

            // Initialize and configure the filter debounce timer.
            _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _filterTimer.Tick += FilterTimer_Tick;

            AddStudentCommand = new RelayCommand(AddStudent);
            EditStudentCommand = new RelayCommand(EditStudent, CanModifyStudent);
            DeleteStudentCommand = new RelayCommand(async param => await DeleteStudent(param), CanModifyStudent);
            ShowGradeCommand = new RelayCommand(ShowGrade, CanModifyStudent);
            UploadStudentCommand = new RelayCommand(UploadWindow);

            // Load students initially.
            LoadStudentsAsync();
        }

        /// <summary>
        /// Resets and starts the debounce timer.
        /// </summary>
        private void ResetFilterTimer()
        {
            _filterTimer.Stop();
            _filterTimer.Start();
        }

        /// <summary>
        /// Called when the debounce timer elapses.
        /// </summary>
        private void FilterTimer_Tick(object sender, EventArgs e)
        {
            _filterTimer.Stop();
            FilterStudents();
        }

        /// <summary>
        /// Loads all students asynchronously and updates the master and filtered collections.
        /// Also populates the Programs and SchoolYears collections.
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
                        SchoolYears.Clear();

                        foreach (var student in studentList)
                        {
                            _allStudents.Add(student);
                            Students.Add(student);

                            // Populate YearLevel collection.
                            if (!Years.Contains(student.Year))
                            {
                                Years.Add(student.Year);
                            }

                            // Populate Semester collection.
                            if (!Semesters.Contains(student.Semester))
                            {
                                Semesters.Add(student.Semester);
                            }

                            // Populate Programs collection.
                            if (!Programs.Contains(student.AcademicProgram.ProgramCode))
                            {
                                Programs.Add(student.AcademicProgram.ProgramCode);
                            }

                            // Populate SchoolYears collection.
                            // Assumes student.SchoolYear exists (e.g. "2022-2023").
                            if (!string.IsNullOrWhiteSpace(student.SchoolYear) && !SchoolYears.Contains(student.SchoolYear))
                            {
                                SchoolYears.Add(student.SchoolYear);
                            }
                        }
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
            var filtered = _allStudents.Where(student =>
                (string.IsNullOrWhiteSpace(SearchText) ||
                 student.FullName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 student.SchoolId.Contains(SearchText)) &&
                (string.IsNullOrWhiteSpace(SelectedSemester) ||
                 student.Semester.Equals(SelectedSemester, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(SelectedYear) ||
                 student.Year.Equals(SelectedYear, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(SelectedSchoolYear) ||
                 student.SchoolYear.Equals(SelectedSchoolYear, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(SelectedProgram) ||
                 student.AcademicProgram.ProgramCode.Equals(SelectedProgram, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            // Update the Students collection on the UI thread.
            Application.Current.Dispatcher.Invoke(() =>
            {
                Students.Clear();
                foreach (var student in filtered)
                {
                    Students.Add(student);
                }
            });
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
