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
using ToastNotifications.Position;
using ToastNotifications.Messages;

namespace GradeVerification.ViewModel
{
    public class StudentDashboardViewModel : INotifyPropertyChanged
    {
        private Notifier _notifier;

        public ObservableCollection<Student> Students { get; set; } = new ObservableCollection<Student>();
        private ObservableCollection<Student> _allStudents = new ObservableCollection<Student>();

        public ObservableCollection<string> Semesters { get; set; } = new ObservableCollection<string> { "First Semester", "Second Semester", "Summer" };
        public ObservableCollection<string> Years { get; set; } = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
        public ObservableCollection<string> Programs { get; set; } = new ObservableCollection<string>();


        private string _searchText;
        private string _selectedSemester;
        private string _selectedYear;
        private string _selectedProgram;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterStudents();
            }
        }

        public string SelectedSemester
        {
            get => _selectedSemester;
            set
            {
                _selectedSemester = value;
                OnPropertyChanged();
                FilterStudents();
            }
        }

        public string SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged();
                FilterStudents();
            }
        }

        public string SelectedProgram
        {
            get => _selectedProgram;
            set
            {
                _selectedProgram = value;
                OnPropertyChanged();
                FilterStudents();
            }
        }

        public ICommand AddStudentCommand { get; }
        public ICommand EditStudentCommand { get; }
        public ICommand DeleteStudentCommand { get; }
        public ICommand ShowGradeCommand { get; }

        public StudentDashboardViewModel()
        {
            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            AddStudentCommand = new RelayCommand(AddStudent);
            EditStudentCommand = new RelayCommand(EditStudent, CanModifyStudent);
            DeleteStudentCommand = new RelayCommand(async param => await DeleteStudent(param), CanModifyStudent);
            ShowGradeCommand = new RelayCommand(ShowGrade, CanModifyStudent);

            LoadStudentsAsync();
        }

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
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading students: {ex.Message}");
            }
        }

        private void FilterStudents()
        {
            Students.Clear();
            foreach (var student in _allStudents)
            {
                bool matchesSearch = string.IsNullOrWhiteSpace(SearchText) ||
                                     student.FullName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     student.SchoolId.ToString().Contains(SearchText);

                bool matchesSemester = string.IsNullOrWhiteSpace(SelectedSemester) || student.Semester == SelectedSemester;
                bool matchesYear = string.IsNullOrWhiteSpace(SelectedYear) || student.Year == SelectedYear;
                bool matchesProgram = string.IsNullOrWhiteSpace(SelectedProgram) || student.AcademicProgram.ProgramCode == SelectedProgram;

                if (matchesSearch && matchesSemester && matchesYear && matchesProgram)
                {
                    Students.Add(student);
                }
            }
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
                var editWindow = new EditStudent(); // Declare the window first
                editWindow.DataContext = new EditStudentViewModel(studentToEdit, editWindow, LoadStudentsAsync); // Now use it
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
                            if (parameter is Student student)
                            {
                                context.Students.Remove(student);
                                context.SaveChanges();
                                LoadStudentsAsync();
                            }
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
