using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Service;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
        private string _middleName;
        private string _lastName;
        private string _email;
        private string _semester;
        private string _year;
        private string _programId;
        private string _status;
        private string _selectedSchoolYear;  // For the ComboBox SelectedItem

        // Dictionary to track if a property has been modified ("touched")
        private readonly Dictionary<string, bool> _propertyTouched = new Dictionary<string, bool>();

        private void MarkPropertyAsTouched(string propertyName)
        {
            if (!_propertyTouched.ContainsKey(propertyName))
                _propertyTouched[propertyName] = true;
        }

        private void MarkAllPropertiesAsTouched()
        {
            var properties = new[]
            {
                nameof(SchoolId), nameof(FirstName), nameof(LastName),
                nameof(Email), nameof(Semester), nameof(Year),
                nameof(ProgramId), nameof(Status), nameof(SelectedSchoolYear)
            };

            foreach (var prop in properties)
            {
                if (!_propertyTouched.ContainsKey(prop))
                    _propertyTouched[prop] = true;
            }
        }

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
                MarkPropertyAsTouched(nameof(SchoolId));
                OnPropertyChanged();
                LoadStudentIfExists();
                (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string SelectedSchoolYear
        {
            get => _selectedSchoolYear;
            set
            {
                _selectedSchoolYear = value;
                MarkPropertyAsTouched(nameof(SelectedSchoolYear));
                OnPropertyChanged();
                (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; MarkPropertyAsTouched(nameof(FirstName)); OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string MiddleName
        {
            get => _middleName;
            set { _middleName = value; MarkPropertyAsTouched(nameof(MiddleName)); OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string LastName
        {
            get => _lastName;
            set { _lastName = value; MarkPropertyAsTouched(nameof(LastName)); OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; MarkPropertyAsTouched(nameof(Email)); OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string Semester
        {
            get => _semester;
            set { _semester = value; MarkPropertyAsTouched(nameof(Semester)); OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string Year
        {
            get => _year;
            set { _year = value; MarkPropertyAsTouched(nameof(Year)); OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string ProgramId
        {
            get => _programId;
            set { _programId = value; MarkPropertyAsTouched(nameof(ProgramId)); OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; MarkPropertyAsTouched(nameof(Status)); OnPropertyChanged(); (SaveStudentCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public ObservableCollection<string> Statuses { get; set; }
        public ObservableCollection<string> Semesters { get; set; }
        public ObservableCollection<string> Years { get; set; }
        public ObservableCollection<AcademicProgram> ProgramList { get; }

        // This collection is for the ComboBox items
        public ObservableCollection<string> SchoolYears { get; set; }

        public ICommand ManageSchoolYearsCommand { get; }
        public ICommand ManageYearLevelCommand { get; }
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
                    notificationLifetime: TimeSpan.FromSeconds(1.5),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(100));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            _onUpdate = onUpdate;
            _programService = new AcademicProgramService();
            _context = new ApplicationDbContext();

            // Initialize collections
            Statuses = new ObservableCollection<string> { "Scholar", "Non-Scholar" };
            Semesters = new ObservableCollection<string> { "First Semester", "Second Semester", "Summer" };
            ProgramList = new ObservableCollection<AcademicProgram>();

            // Initialize SchoolYears collection and load data from the database
            SchoolYears = new ObservableCollection<string>();
            Years = new ObservableCollection<string>();

            LoadSchoolYears();

            LoadPrograms();

            LoadYearLevels();

            SaveStudentCommand = new RelayCommand(async param => await SaveStudent(), CanSaveStudent);
            BackCommand = new RelayCommand(Back);
            ManageSchoolYearsCommand = new RelayCommand(ManageSchoolYears);
            ManageYearLevelCommand = new RelayCommand(ManageYearLevel);
        }

        private async void LoadSchoolYears()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var years = await context.SchoolYears
                        .OrderByDescending(y => y.SchoolYears)
                        .ToListAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SchoolYears.Clear();
                        foreach (var year in years)
                        {
                            // Assuming the model has a property named SchoolYears that is a string.
                            SchoolYears.Add(year.SchoolYears);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading school years: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                MiddleName = existingStudent.MiddleName;
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

        private async void LoadYearLevels()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var levels = await context.YearLevels
                        .OrderBy(y => y.LevelName)
                        .ToListAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Years.Clear();
                        foreach (var level in levels)
                        {
                            Years.Add(level.LevelName);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading year levels: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManageSchoolYears(object parameter)
        {
            var manageWindow = new ManageSchoolYears();
            manageWindow.DataContext = new ManageSchoolYearsViewModel();
            manageWindow.Show();
        }

        private void ManageYearLevel(object parameter)
        {
            var manageWindow = new ManageYearLevel();
            manageWindow.DataContext = new ManageYearLevelViewModel();
            manageWindow.Show();
        }

        private async Task SaveStudent()
        {
            MarkAllPropertiesAsTouched();

            if (!CanSaveStudent(null))
            {
                ShowErrorNotification("Please fill in all fields correctly.");
                return;
            }

            try
            {
                var existingStudent = await _context.Students
                    .Where(s => s.SchoolId == SchoolId &&
                                s.FirstName == FirstName &&
                                s.MiddleName == MiddleName &&
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

                // Include the SchoolYear property using SelectedSchoolYear
                var newStudent = new Student
                {
                    SchoolId = SchoolId,
                    FirstName = FirstName,
                    MiddleName = MiddleName,
                    LastName = LastName,
                    Email = Email,
                    Semester = Semester,
                    Year = Year,
                    ProgramId = ProgramId,
                    Status = Status,
                    SchoolYear = SelectedSchoolYear  // Added property
                };

                _context.Students.Add(newStudent);
                await _context.SaveChangesAsync();

                // Subject Enrollment Logic:
                // Enroll the student in all subjects that match their program, year, and semester.
                await EnrollStudentSubjects(newStudent);

                ClearForm();
                LoadPrograms();
                ShowSuccessNotification("Student saved successfully!");

                string currentUsername = Environment.UserName;
                _activityLogService.LogActivity("Student", "Add", $"Student added by {currentUsername}");
                _onUpdate?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.InnerException?.Message ?? ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowErrorNotification("Error saving student.");
            }
        }

        /// <summary>
        /// Enrolls the newly added student in subjects based on their program, year, and semester.
        /// Assumes that:
        /// 1. There is a Subjects DbSet in _context.
        /// 2. Each Subject has properties like ProgramId, Year, and Semester.
        /// 3. There is an Enrollment entity with at least StudentId, SubjectId, and EnrolledOn properties.
        /// </summary>
        private async Task EnrollStudentSubjects(Student student)
        {
            // Skip enrollment if the student is a non-scholar.
            if (student.Status.Equals("Non-Scholar", StringComparison.OrdinalIgnoreCase))
            {
                _notifier.ShowInformation("Student is a non-scholar. Enrollment skipped.");
                return;
            }

            // Retrieve the list of subjects that match the student's details.
            var subjectsToEnroll = await _context.Subjects
                .Where(s => s.ProgramId == student.ProgramId &&
                            s.Year == student.Year &&
                            s.Semester == student.Semester)
                .ToListAsync();

            if (subjectsToEnroll.Any())
            {
                foreach (var subject in subjectsToEnroll)
                {
                    var enrollment = new Grade
                    {
                        StudentId = student.Id, // Ensure Student.Id is populated after SaveChangesAsync
                        SubjectId = subject.SubjectId,
                        ProfessorName = subject.Professor,
                    };
                    _context.Grade.Add(enrollment);
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                // Optionally notify if no subjects were found for enrollment.
                _notifier.ShowInformation("No subjects available for enrollment based on the selected criteria.");
            }
        }

        private void ClearForm()
        {
            SchoolId = string.Empty;
            FirstName = string.Empty;
            MiddleName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Semester = null;
            Year = null;
            ProgramId = null;
            Status = null;
            SelectedSchoolYear = null;
            _propertyTouched.Clear();
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
                if (!_propertyTouched.ContainsKey(columnName))
                    return null;

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
                    case nameof(SelectedSchoolYear):
                        if (string.IsNullOrWhiteSpace(SelectedSchoolYear))
                            error = "School Year selection is required.";
                        break;
                }
                return error;
            }
        }

        public string Error => null;

        private bool CanSaveStudent(object parameter)
        {
            return string.IsNullOrEmpty(this[nameof(SchoolId)]) &&
                   string.IsNullOrEmpty(this[nameof(FirstName)]) &&
                   string.IsNullOrEmpty(this[nameof(LastName)]) &&
                   string.IsNullOrEmpty(this[nameof(Email)]) &&
                   string.IsNullOrEmpty(this[nameof(Semester)]) &&
                   string.IsNullOrEmpty(this[nameof(Year)]) &&
                   string.IsNullOrEmpty(this[nameof(ProgramId)]) &&
                   string.IsNullOrEmpty(this[nameof(Status)]) &&
                   string.IsNullOrEmpty(this[nameof(SelectedSchoolYear)]);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
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
