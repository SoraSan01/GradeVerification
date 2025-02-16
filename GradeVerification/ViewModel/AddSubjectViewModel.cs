using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows;
using GradeVerification.View.Admin.Windows;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;

public class AddSubjectViewModel : INotifyPropertyChanged
{
    private Notifier _notifier;
    private readonly Action _onUpdate;

    private readonly AcademicProgramService _programService;
    private readonly ApplicationDbContext _context;

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

            ShowSuccessNotification("Succesfully A Added Subject!");

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
        return !string.IsNullOrEmpty(SubjectCode) &&
               !string.IsNullOrEmpty(SubjectName) &&
               Units > 0 &&
               !string.IsNullOrEmpty(SelectedYear) &&
               !string.IsNullOrEmpty(SelectedProgramID) &&
               !string.IsNullOrEmpty(SelectedSemester) &&
               !string.IsNullOrEmpty(ProfessorName) &&
               !string.IsNullOrEmpty(Schedule);
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
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
