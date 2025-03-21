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
    public class AddSubjectViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Notifier _notifier;
        private readonly AcademicProgramService _programService;
        private readonly ApplicationDbContext _context;
        private readonly ActivityLogService _activityLogService;

        private string _subjectCode;
        private string _subjectName;
        private int _units;
        private string _selectedYear;
        private string _selectedProgramID;
        private string _selectedSemester;
        private Professor _selectedProfessor; // Changed from string to Professor
        private string _schedule;

        // Flag to control when validation errors should be shown.
        private bool _isSubmitted = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public AddSubjectViewModel(Action onUpdate)
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

            // Initialize collections for ComboBoxes
            YearList = new ObservableCollection<YearLevel>();
            SemesterList = new ObservableCollection<string> { "First Semester", "Second Semester" };

            ProgramList = new ObservableCollection<AcademicProgram>();
            Professors = new ObservableCollection<Professor>();  // For the professor ComboBox

            _programService = new AcademicProgramService();

            SaveSubjectCommand = new RelayCommand(async (param) => await SaveSubject(), (param) => CanSaveSubject());
            CancelCommand = new RelayCommand(Cancel);
            ManageProfessorCommand = new RelayCommand(ManageProfessors);

            LoadProfessors();
            LoadPrograms();
            LoadYears();

            _onUpdate = onUpdate;
        }

        // New command to open the professor management window
        public ICommand ManageProfessorCommand { get; }

        private void ManageProfessors(object parameter)
        {
            // Opens a window where the user can add/delete professors
            var professorWindow = new ManageProfessors();
            professorWindow.DataContext = new ManageProfessorViewModel();
            professorWindow.Show();

            LoadProfessors();
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

        private async void LoadProfessors()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    Professors.Clear();
                    var professors = await context.Professors.ToListAsync();
                    foreach (var professor in professors)
                    {
                        Professors.Add(professor);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading professors: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadYears()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    YearList.Clear();
                    var years = await context.YearLevels.ToListAsync();
                    foreach (var year in years)
                    {
                        YearList.Add(year);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading years: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }   

        public string SubjectCode
        {
            get => _subjectCode;
            set { _subjectCode = value; OnPropertyChanged(); }
        }

        public string SubjectName
        {
            get => _subjectName;
            set { _subjectName = value; OnPropertyChanged(); }
        }

        public int Units
        {
            get => _units;
            set { _units = value; OnPropertyChanged(); }
        }

        public string SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(); }
        }

        public string SelectedProgramID
        {
            get => _selectedProgramID;
            set { _selectedProgramID = value; OnPropertyChanged(); }
        }

        public string SelectedSemester
        {
            get => _selectedSemester;
            set { _selectedSemester = value; OnPropertyChanged(); }
        }

        // Updated property for professor
        public Professor SelectedProfessor
        {
            get => _selectedProfessor;
            set { _selectedProfessor = value; OnPropertyChanged(); }
        }

        public string Schedule
        {
            get => _schedule;
            set { _schedule = value; OnPropertyChanged(); }
        }

        public ObservableCollection<YearLevel> YearList { get; }
        public ObservableCollection<AcademicProgram> ProgramList { get; }
        public ObservableCollection<string> SemesterList { get; }
        public ObservableCollection<Professor> Professors { get; }  // For professor ComboBox

        public ICommand SaveSubjectCommand { get; }
        public ICommand CancelCommand { get; }

        // IDataErrorInfo implementation for validation
        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                // Only show errors if the user has tried to submit.
                if (!_isSubmitted)
                    return null;

                string error = null;
                switch (columnName)
                {
                    case nameof(SubjectCode):
                        if (string.IsNullOrWhiteSpace(SubjectCode))
                            error = "Subject Code is required.";
                        break;
                    case nameof(SubjectName):
                        if (string.IsNullOrWhiteSpace(SubjectName))
                            error = "Subject Name is required.";
                        break;
                    case nameof(Units):
                        if (Units <= 0)
                            error = "Units must be greater than zero.";
                        break;
                    case nameof(SelectedYear):
                        if (string.IsNullOrWhiteSpace(SelectedYear))
                            error = "Year selection is required.";
                        break;
                    case nameof(SelectedProgramID):
                        if (string.IsNullOrWhiteSpace(SelectedProgramID))
                            error = "Program selection is required.";
                        break;
                    case nameof(SelectedSemester):
                        if (string.IsNullOrWhiteSpace(SelectedSemester))
                            error = "Semester selection is required.";
                        break;
                    case nameof(SelectedProfessor):
                        if (SelectedProfessor == null)
                            error = "Professor selection is required.";
                        break;
                    case nameof(Schedule):
                        if (string.IsNullOrWhiteSpace(Schedule))
                            error = "Schedule is required.";
                        break;
                }
                return error;
            }
        }

        private readonly Action _onUpdate;

        private async Task SaveSubject()
        {
            // Mark that the user has tried to submit the form.
            _isSubmitted = true;

            // Re-check if we can save
            if (!CanSaveSubject())
            {
                ShowErrorNotification("Please fill in all required fields.");
                return;
            }

            try
            {
                var newSubject = new Subject
                {
                    SubjectCode = SubjectCode,
                    SubjectName = SubjectName,
                    Units = Units,
                    Year = SelectedYear,
                    ProgramId = SelectedProgramID,
                    Semester = SelectedSemester,
                    // Use the name of the selected professor if available.
                    Professor = SelectedProfessor != null ? SelectedProfessor.Name : string.Empty,
                    Schedule = Schedule
                };

                _context.Subjects.Add(newSubject);
                await _context.SaveChangesAsync();

                ShowSuccessNotification("Successfully added the subject!");

                string currentUsername = Environment.UserName;
                _activityLogService.LogActivity("Subject", "Add", $"Subject added by {currentUsername}");

                _onUpdate?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving subject: {ex.Message}");
            }
        }

        private void Cancel(object obj)
        {
            Application.Current.Windows.OfType<AddSubject>().FirstOrDefault()?.Close();
        }

        private bool CanSaveSubject()
        {
            // Ensure all fields are valid (i.e. no validation errors exist)
            return string.IsNullOrEmpty(this[nameof(SubjectCode)]) &&
                   string.IsNullOrEmpty(this[nameof(SubjectName)]) &&
                   string.IsNullOrEmpty(this[nameof(Units)]) &&
                   string.IsNullOrEmpty(this[nameof(SelectedYear)]) &&
                   string.IsNullOrEmpty(this[nameof(SelectedProgramID)]) &&
                   string.IsNullOrEmpty(this[nameof(SelectedSemester)]) &&
                   string.IsNullOrEmpty(this[nameof(SelectedProfessor)]) &&
                   string.IsNullOrEmpty(this[nameof(Schedule)]);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            // Optionally refresh the Save command's state
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
