using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Service;
using GradeVerification.View.Admin.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class EditStudentViewModel : INotifyPropertyChanged
    {
        private Notifier _notifier;
        private readonly ActivityLogService _activityLogService;

        private string _schoolId;
        private string _firstName;
        private string _middleName; // Added MiddleName field
        private string _lastName;
        private string _studentId;
        private string _email;
        private string _semester;
        private string _year;
        private string _programId;
        private string _selectedStatus;
        private string _schoolYear; // New field for School Year

        private readonly EditStudent _editWindow;
        private readonly Action _onUpdate;
        private readonly AcademicProgramService _programService;

        public event PropertyChangedEventHandler PropertyChanged;

        public EditStudentViewModel(Student student, EditStudent editWindow, Action onUpdate)
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

            _editWindow = editWindow ?? throw new ArgumentNullException(nameof(editWindow));
            _onUpdate = onUpdate ?? throw new ArgumentNullException(nameof(onUpdate));
            _programService = new AcademicProgramService();

            // Initialize properties, including the new SchoolYear property
            SchoolId = student.SchoolId;
            FirstName = student.FirstName;
            MiddleName = student.MiddleName; // Initialize MiddleName from the student object
            LastName = student.LastName;
            StudentId = student.Id;
            Email = student.Email;
            Semester = student.Semester;
            Year = student.Year;
            ProgramId = student.ProgramId;
            SelectedStatus = student.Status;
            SchoolYear = student.SchoolYear; // Initialize SchoolYear

            // Dropdown options
            Statuses = new ObservableCollection<string> { "Scholar", "Non-Scholar" };
            Semesters = new ObservableCollection<string> { "First Semester", "Second Semester" };
            Years = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
            ProgramList = new ObservableCollection<AcademicProgram>();

            LoadData();

            // Commands
            UpdateStudentCommand = new RelayCommand(UpdateStudent);
            BackCommand = new RelayCommand(Back);
        }

        // Properties with INotifyPropertyChanged
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
        public string MiddleName
        {
            get => _middleName;
            set { _middleName = value; OnPropertyChanged(); }
        }
        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(); }
        }
        public string StudentId
        {
            get => _studentId;
            set { _studentId = value; OnPropertyChanged(); }
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
        public string SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); }
        }
        public string SchoolYear
        {
            get => _schoolYear;
            set { _schoolYear = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Semesters { get; }
        public ObservableCollection<string> Years { get; }
        public ObservableCollection<AcademicProgram> ProgramList { get; }
        public ObservableCollection<string> Statuses { get; }

        public ICommand UpdateStudentCommand { get; }
        public ICommand BackCommand { get; }

        private void UpdateStudent(object parameter)
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var student = context.Students.FirstOrDefault(s => s.Id == StudentId);

                    if (student == null)
                    {
                        MessageBox.Show("Student not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Update student details including MiddleName and SchoolYear
                    student.SchoolId = SchoolId;
                    student.FirstName = FirstName;
                    student.MiddleName = MiddleName; // Update MiddleName
                    student.LastName = LastName;
                    student.Email = Email;
                    student.Semester = Semester;
                    student.Year = Year;
                    student.ProgramId = ProgramId;
                    student.Status = SelectedStatus;
                    // SchoolYear is not updated since it's read-only in this view

                    context.SaveChanges();
                    ShowSuccessNotification("Student information updated successfully.");

                    string currentUsername = Environment.UserName;
                    _activityLogService.LogActivity("Student", $"Student Edit by {currentUsername}", "Student");
                }

                _onUpdate?.Invoke();
                _editWindow.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while updating student: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowErrorNotification("Error updating student.");
            }
        }

        private void LoadData()
        {
            try
            {
                var programs = _programService.GetPrograms() ?? new System.Collections.Generic.List<AcademicProgram>();

                ProgramList.Clear();
                foreach (var program in programs)
                {
                    ProgramList.Add(program);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load programs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back(object parameter)
        {
            _editWindow.Close();
        }

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
