using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Service;
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
using ToastNotifications.Position;
using ToastNotifications.Messages;
using GradeVerification.View.Admin.Windows;

public class AddSubjectViewModel : INotifyPropertyChanged, IDataErrorInfo
{
    private Notifier _notifier;
    private readonly Action _onUpdate;

    private readonly AcademicProgramService _programService;
    private readonly ApplicationDbContext _context;

    private readonly ActivityLogService _activityLogService;

    private string _subjectCode;
    private string _subjectName;
    private int _units;
    private string _selectedYear;
    private string _selectedProgramID;
    private string _selectedSemester;
    private string _professorName;
    private string _schedule;

    public event PropertyChangedEventHandler PropertyChanged;

    public AddSubjectViewModel(ApplicationDbContext dbContext, Action onUpdate)
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

        YearList = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
        SemesterList = new ObservableCollection<string> { "First Semester", "Second Semester" };

        ProgramList = new ObservableCollection<AcademicProgram>();

        _programService = new AcademicProgramService();
        _context = dbContext;

        LoadPrograms();

        SaveSubjectCommand = new RelayCommand(async (param) => await SaveSubject(), (param) => CanSaveSubject());
        CancelCommand = new RelayCommand(Cancel);
        _onUpdate = onUpdate;
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

    public string ProfessorName
    {
        get => _professorName;
        set { _professorName = value; OnPropertyChanged(); }
    }

    public string Schedule
    {
        get => _schedule;
        set { _schedule = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> YearList { get; }
    public ObservableCollection<AcademicProgram> ProgramList { get; }
    public ObservableCollection<string> SemesterList { get; }

    public ICommand SaveSubjectCommand { get; }
    public ICommand CancelCommand { get; }

    // IDataErrorInfo implementation for validation
    public string Error => null;

    public string this[string columnName]
    {
        get
        {
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
                case nameof(ProfessorName):
                    if (string.IsNullOrWhiteSpace(ProfessorName))
                        error = "Professor Name is required.";
                    break;
                case nameof(Schedule):
                    if (string.IsNullOrWhiteSpace(Schedule))
                        error = "Schedule is required.";
                    break;
            }
            return error;
        }
    }

    private async Task SaveSubject()
    {
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
                Professor = ProfessorName,
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
               string.IsNullOrEmpty(this[nameof(ProfessorName)]) &&
               string.IsNullOrEmpty(this[nameof(Schedule)]);
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        // Optionally, you can also refresh the state of the Save command here if your RelayCommand supports it.
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
