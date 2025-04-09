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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class EditSubjectViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Notifier _notifier;
        private string _subjectCode;
        private string _subjectName;
        private int _units;
        private string _selectedYear;
        private string _selectedProgramID;
        private string _selectedSemester;
        private string _schedule;
        // Remove the old string-based professor property.
        // Instead, use the following properties:
        private Professor _selectedProfessor;

        private readonly EditSubject _editWindow;
        private readonly Action _onUpdate;
        private readonly Subject _originalSubject;

        public EditSubjectViewModel(Subject subject, EditSubject editWindow, Action onUpdate)
        {
            _originalSubject = subject;
            _editWindow = editWindow ?? throw new ArgumentNullException(nameof(editWindow));
            _onUpdate = onUpdate ?? throw new ArgumentNullException(nameof(onUpdate));

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

            // Initialize properties from the existing subject record.
            SubjectCode = subject.SubjectCode;
            SubjectName = subject.SubjectName;
            Units = subject.Units;
            SelectedYear = subject.Year;
            SelectedProgramID = subject.ProgramId;
            SelectedSemester = subject.Semester;
            // For backward compatibility, if the subject already has a professor name, you can try to select that professor.
            // Otherwise, SelectedProfessor remains null.
            // (You might consider matching by name from the loaded professors.)
            _schedule = subject.Schedule;

            // Initialize dropdown lists.
            YearList = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
            SemesterList = new ObservableCollection<string> { "First Semester", "Second Semester" };
            ProgramList = new ObservableCollection<AcademicProgram>();
            LoadPrograms();

            // Initialize Professors list.
            Professors = new ObservableCollection<Professor>();
            LoadProfessors();

            // Create commands with CanExecute based on validation.
            SaveCommand = new RelayCommand(SaveSubject, _ => !HasErrors());
            CancelCommand = new RelayCommand(Cancel);
            ViewProfessorCommand = new RelayCommand(ViewProfessors);
        }

        // New command to view/manage professors.
        public ICommand ViewProfessorCommand { get; }

        private void ViewProfessors(object parameter)
        {
            using (var context = new ApplicationDbContext())
            {
                var window = new ManageProfessors();
                window.DataContext = new ManageProfessorViewModel();
                window.Show();
                // Reload professors after the management window closes.
                LoadProfessors();
            }
        }

        private void LoadPrograms()
        {
            try
            {
                var programs = new AcademicProgramService().GetPrograms() ?? Enumerable.Empty<AcademicProgram>();
                ProgramList.Clear();
                foreach (var program in programs)
                    ProgramList.Add(program);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load programs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProfessors()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var profs = context.Professors.OrderBy(p => p.Name).ToList();
                    Professors.Clear();
                    foreach (var p in profs)
                    {
                        Professors.Add(p);
                        // Optional: if the current subject's professor name matches, select it.
                        if (p.Name.Equals(_originalSubject.Professor, StringComparison.OrdinalIgnoreCase))
                        {
                            SelectedProfessor = p;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load professors: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Properties with validation support.
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

        // New Professor property (ComboBox)
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

        // Collections for dropdowns.
        public ObservableCollection<string> YearList { get; }
        public ObservableCollection<string> SemesterList { get; }
        public ObservableCollection<AcademicProgram> ProgramList { get; }
        public ObservableCollection<Professor> Professors { get; }

        // Commands.
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // IDataErrorInfo implementation.
        public string Error => null;

        private bool _hasAttemptedSave = false; // Track if user attempted to save.

        public string this[string columnName]
        {
            get
            {
                if (!_hasAttemptedSave) return null; // Skip validation if save not attempted.

                switch (columnName)
                {
                    case nameof(SubjectCode):
                        if (string.IsNullOrWhiteSpace(SubjectCode))
                            return "Subject Code is required.";
                        break;
                    case nameof(SubjectName):
                        if (string.IsNullOrWhiteSpace(SubjectName))
                            return "Subject Name is required.";
                        break;
                    case nameof(Units):
                        if (Units <= 0)
                            return "Units must be greater than zero.";
                        break;
                    case nameof(SelectedYear):
                        if (string.IsNullOrWhiteSpace(SelectedYear))
                            return "Year selection is required.";
                        break;
                    case nameof(SelectedProgramID):
                        if (string.IsNullOrWhiteSpace(SelectedProgramID))
                            return "Program selection is required.";
                        break;
                    case nameof(SelectedSemester):
                        if (string.IsNullOrWhiteSpace(SelectedSemester))
                            return "Semester selection is required.";
                        break;
                    case nameof(SelectedProfessor):
                        if (SelectedProfessor == null)
                            return "Professor selection is required.";
                        break;
                    case nameof(Schedule):
                        if (string.IsNullOrWhiteSpace(Schedule))
                            return "Schedule is required.";
                        break;
                }
                return null;
            }
        }

        private bool HasErrors()
        {
            return _hasAttemptedSave && (
                !string.IsNullOrEmpty(this[nameof(SubjectCode)]) ||
                !string.IsNullOrEmpty(this[nameof(SubjectName)]) ||
                !string.IsNullOrEmpty(this[nameof(Units)]) ||
                !string.IsNullOrEmpty(this[nameof(SelectedYear)]) ||
                !string.IsNullOrEmpty(this[nameof(SelectedProgramID)]) ||
                !string.IsNullOrEmpty(this[nameof(SelectedSemester)]) ||
                !string.IsNullOrEmpty(this[nameof(SelectedProfessor)])
            );
        }

        // Saves the subject after validation.
        private async void SaveSubject(object parameter)
        {
            _hasAttemptedSave = true; // Activate validation.

            OnPropertyChanged(nameof(SubjectCode)); // Trigger UI validation update.
            OnPropertyChanged(nameof(SubjectName));
            OnPropertyChanged(nameof(Units));
            OnPropertyChanged(nameof(SelectedYear));
            OnPropertyChanged(nameof(SelectedProgramID));
            OnPropertyChanged(nameof(SelectedSemester));
            OnPropertyChanged(nameof(SelectedProfessor));
            OnPropertyChanged(nameof(Schedule));

            if (HasErrors())
            {
                ShowErrorNotification("Please correct the errors before saving.");
                return;
            }

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var subjectToUpdate = await context.Subjects.FindAsync(_originalSubject.SubjectId);
                    if (subjectToUpdate != null)
                    {
                        subjectToUpdate.SubjectCode = SubjectCode;
                        subjectToUpdate.SubjectName = SubjectName;
                        subjectToUpdate.Units = Units;
                        subjectToUpdate.Year = SelectedYear;
                        subjectToUpdate.ProgramId = SelectedProgramID;
                        subjectToUpdate.Semester = SelectedSemester;
                        subjectToUpdate.Professor = SelectedProfessor?.Name ?? string.Empty;
                        subjectToUpdate.Schedule = Schedule;
                        await context.SaveChangesAsync();
                    }
                }
                ShowSuccessNotification("Subject updated successfully!");
                _onUpdate?.Invoke();
                _editWindow.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating subject: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowErrorNotification("Error updating subject.");
            }
        }

        private void Cancel(object parameter)
        {
            _editWindow.Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
