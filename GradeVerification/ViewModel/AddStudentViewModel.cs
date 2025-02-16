using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Service;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public class AddStudentViewModel : INotifyPropertyChanged
    {
        private Notifier _notifier;

        private readonly AcademicProgramService _programService;
        private readonly ApplicationDbContext _context;

        private string _studentId;
        private string _schoolId;
        private string _firstName;
        private string _lastName;
        private string _email;
        private string _semester;
        private string _year;
        private string _programId;
        private string _status;


        public string StudentId
        {
            get => _studentId;
            set { _studentId = value; OnPropertyChanged(); }
        }

        public string SchoolId
        {
            get => _schoolId;
            set { _schoolId = value; OnPropertyChanged(); }
        }

        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(); }
        }

        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Semester
        {
            get => _semester;
            set { _semester = value; OnPropertyChanged(); }
        }

        public string Year
        {
            get => _year;
            set { _year = value; OnPropertyChanged(); }
        }

        public string ProgramId
        {
            get => _programId;
            set { _programId = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Statuses { get; set; }
        public ObservableCollection<string> Semesters { get; set; }
        public ObservableCollection<string> Years { get; set; }
        public ObservableCollection<AcademicProgram> ProgramList { get; }

        public ICommand SaveStudentCommand { get; }
        public ICommand BackCommand { get; }

        private readonly Action _onUpdate;

        public AddStudentViewModel(Action onUpdate)
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

            _onUpdate = onUpdate;

            _programService = new AcademicProgramService();
            _context = new ApplicationDbContext();

            Statuses = new ObservableCollection<string> { "Scholar", "Non-Scholar" };
            Semesters = new ObservableCollection<string> { "First Semester", "Second Semester" };
            Years = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
            ProgramList = new ObservableCollection<AcademicProgram>();

            LoadPrograms();

            SaveStudentCommand = new RelayCommand(async param => await SaveStudent());
            BackCommand = new RelayCommand(Back);
        }

        private void LoadPrograms()
        {
            ProgramList.Clear();
            var programs = _programService.GetPrograms();
            foreach (var program in programs)
            {
                ProgramList.Add(program);
            }
        }

        private async Task SaveStudent()
        {
            try
            {
                // Validate input fields
                if (string.IsNullOrWhiteSpace(SchoolId) ||
                    string.IsNullOrWhiteSpace(FirstName) ||
                    string.IsNullOrWhiteSpace(LastName) ||
                    string.IsNullOrWhiteSpace(Email) ||
                    string.IsNullOrWhiteSpace(Semester) ||
                    string.IsNullOrWhiteSpace(Year) ||
                    string.IsNullOrWhiteSpace(ProgramId) ||
                    string.IsNullOrWhiteSpace(Status))
                {
                    ShowErrorNotification("Please fill in all fields.");
                    return;
                }

                // Check for duplicate email
                if (_context.Students.Any(s => s.Email == Email))
                {
                    ShowErrorNotification("A student with this email already exists.");
                    return;
                }

                // Create new student
                var newStudent = new Student
                {
                    SchoolId = SchoolId,
                    FirstName = FirstName,
                    LastName = LastName,
                    Email = Email,
                    Semester = Semester,
                    Year = Year,
                    ProgramId = ProgramId,
                    Status = Status
                };

                _context.Students.Add(newStudent);
                await _context.SaveChangesAsync(); // Save the student to generate the Id

                // Assign subjects
                if (Status.Equals("Scholar", StringComparison.OrdinalIgnoreCase))
                {
                    var subjectsToEnroll = await _context.Subjects
                        .Where(s => s.ProgramId == newStudent.ProgramId && s.Year == newStudent.Year && s.Semester == newStudent.Semester)
                        .ToListAsync(); // Use ToListAsync for async operation

                    if (subjectsToEnroll.Count == 0)
                    {
                        ShowErrorNotification("No subjects found for the selected program, year, and semester.");
                        return;
                    }

                    foreach (var subject in subjectsToEnroll)
                    {
                        var newGrade = new Grade
                        {
                            StudentId = newStudent.Id, // Use the saved student ID
                            SubjectId = subject.SubjectId,
                            Score = null // Initially null until graded
                        };

                        _context.Grade.Add(newGrade);
                    }

                    await _context.SaveChangesAsync(); // Save the grades
                }
                ClearForm();
                LoadPrograms(); // Refresh UI

                ShowSuccessNotification("Student saved successfully!");

                _onUpdate?.Invoke(); // Notify main view to refresh UI
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.InnerException?.Message ?? ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowErrorNotification("Error saving student.");
            }
        }

        private void ClearForm()
        {
            StudentId = string.Empty;
            SchoolId = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Semester = null;  // Reset to null instead of empty string for dropdowns
            Year = null;
            ProgramId = null;
            Status = null;
        }

        private void Back(object parameter)
        {
            Application.Current.Windows.OfType<AddStudent>().FirstOrDefault()?.Close();
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