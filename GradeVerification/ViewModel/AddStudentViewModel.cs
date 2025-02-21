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
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;
using System.Text.RegularExpressions;

namespace GradeVerification.ViewModel
{
    public class AddStudentViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Notifier _notifier;
        private readonly AcademicProgramService _programService;
        private readonly ApplicationDbContext _context;

        private readonly ActivityLogService _activityLogService;


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
            set
            {
                _schoolId = value;
                OnPropertyChanged();
                LoadStudentIfExists();
                (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string Semester
        {
            get => _semester;
            set { _semester = value; OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string Year
        {
            get => _year;
            set { _year = value; OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string ProgramId
        {
            get => _programId;
            set { _programId = value; OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
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

            _activityLogService = new ActivityLogService();

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
            Semesters = new ObservableCollection<string> { "First Semester", "Second Semester", "Summer" };
            Years = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
            ProgramList = new ObservableCollection<AcademicProgram>();

            LoadPrograms();

            // Use a can-execute delegate that checks our validations.
            SaveStudentCommand = new RelayCommand(async param => await SaveStudent(), CanSaveStudent);
            BackCommand = new RelayCommand(Back);
        }

        private async void LoadStudentIfExists()
        {
            if (string.IsNullOrWhiteSpace(SchoolId)) return;

            var existingStudent = await _context.Students
                .Where(s => s.SchoolId == SchoolId)
                .FirstOrDefaultAsync();

            if (existingStudent != null)
            {
                FirstName = existingStudent.FirstName;
                LastName = existingStudent.LastName;
                Email = existingStudent.Email;

                // Clear these for new entry details
                ProgramId = null;
                Semester = null;
                Year = null;
                Status = null;

                _notifier.ShowInformation("Existing student found. Some fields have been auto-filled.");
            }
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
                // Double-check validation (though the command should be disabled if invalid)
                if (!CanSaveStudent(null))
                {
                    ShowErrorNotification("Please fill in all fields correctly.");
                    return;
                }

                // Check if a student with the same SchoolId, FirstName, LastName, and Email exists
                var existingStudent = await _context.Students
                    .Where(s => s.SchoolId == SchoolId &&
                                s.FirstName == FirstName &&
                                s.LastName == LastName &&
                                s.Email == Email)
                    .FirstOrDefaultAsync();

                if (existingStudent != null)
                {
                    if (existingStudent.Year == Year &&
                        existingStudent.ProgramId == ProgramId &&
                        existingStudent.Semester == Semester &&
                        existingStudent.Status == Status)
                    {
                        ShowErrorNotification("A student with these details already exists with the same program, year, semester, and status.");
                        return;
                    }
                }

                // Create new student record
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
                await _context.SaveChangesAsync(); // Save to generate the student Id

                // Enroll subjects if applicable (scholar or non-summer semester)
                if (Status.Equals("Scholar", StringComparison.OrdinalIgnoreCase) ||
                    (!Semester.Equals("Summer", StringComparison.OrdinalIgnoreCase)))
                {
                    var subjectsToEnroll = await _context.Subjects
                        .Where(s => s.ProgramId == newStudent.ProgramId && s.Year == newStudent.Year && s.Semester == newStudent.Semester)
                        .ToListAsync();

                    if (subjectsToEnroll.Count == 0)
                    {
                        ShowErrorNotification("No subjects found for the selected program, year, and semester.");
                        return;
                    }

                    foreach (var subject in subjectsToEnroll)
                    {
                        var newGrade = new Grade
                        {
                            StudentId = newStudent.Id,
                            SubjectId = subject.SubjectId,
                            Score = null
                        };

                        _context.Grade.Add(newGrade);
                    }

                    await _context.SaveChangesAsync();
                }

                ClearForm();
                LoadPrograms(); // Refresh program list if needed
                ShowSuccessNotification("Student saved successfully!");

                string currentUsername = Environment.UserName;

                _activityLogService.LogActivity("Student", "Add", $"Student added by {currentUsername}");

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
            SchoolId = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Semester = null;
            Year = null;
            ProgramId = null;
            Status = null;
        }

        private void Back(object parameter)
        {
            Application.Current.Windows.OfType<AddStudent>().FirstOrDefault()?.Close();
        }

        #region IDataErrorInfo Implementation

        public string this[string columnName]
        {
            get
            {
                string error = null;
                switch (columnName)
                {
                    case nameof(SchoolId):
                        if (string.IsNullOrWhiteSpace(SchoolId))
                            error = "Student ID is required.";
                        break;
                    case nameof(FirstName):
                        if (string.IsNullOrWhiteSpace(FirstName))
                            error = "First name is required.";
                        break;
                    case nameof(LastName):
                        if (string.IsNullOrWhiteSpace(LastName))
                            error = "Last name is required.";
                        break;
                    case nameof(Email):
                        if (string.IsNullOrWhiteSpace(Email))
                            error = "Email is required.";
                        else if (!IsValidEmail(Email))
                            error = "Invalid email format.";
                        break;
                    case nameof(Semester):
                        if (string.IsNullOrWhiteSpace(Semester))
                            error = "Semester selection is required.";
                        break;
                    case nameof(Year):
                        if (string.IsNullOrWhiteSpace(Year))
                            error = "Year selection is required.";
                        break;
                    case nameof(ProgramId):
                        if (string.IsNullOrWhiteSpace(ProgramId))
                            error = "Program selection is required.";
                        break;
                    case nameof(Status):
                        if (string.IsNullOrWhiteSpace(Status))
                            error = "Status selection is required.";
                        break;
                }
                return error;
            }
        }

        public string Error => null;

        private bool CanSaveStudent(object parameter)
        {
            // All validation errors must be null or empty
            return string.IsNullOrEmpty(this[nameof(SchoolId)]) &&
                   string.IsNullOrEmpty(this[nameof(FirstName)]) &&
                   string.IsNullOrEmpty(this[nameof(LastName)]) &&
                   string.IsNullOrEmpty(this[nameof(Email)]) &&
                   string.IsNullOrEmpty(this[nameof(Semester)]) &&
                   string.IsNullOrEmpty(this[nameof(Year)]) &&
                   string.IsNullOrEmpty(this[nameof(ProgramId)]) &&
                   string.IsNullOrEmpty(this[nameof(Status)]);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                // Using MailAddress to validate email format
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

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
