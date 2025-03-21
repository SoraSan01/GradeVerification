using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using GradeVerification.Repository;
using GradeVerification.Model;
using System.Windows.Threading;
using GradeVerification.Commands;

namespace GradeVerification.ViewModel
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly IRepository<Student> _studentRepository;
        private readonly IRepository<Subject> _subjectRepository;
        private readonly IRepository<AcademicProgram> _programRepository;

        private int _studentsCount;
        private int _coursesCount;
        private int _programsCount;

        public int StudentsCount
        {
            get => _studentsCount;
            set { _studentsCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(StudentsCountDisplay)); }
        }

        public int CoursesCount
        {
            get => _coursesCount;
            set { _coursesCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(CoursesCountDisplay)); }
        }

        public int ProgramsCount
        {
            get => _programsCount;
            set { _programsCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgramsCountDisplay)); }
        }

        // Formatted display properties with number separators.
        public string StudentsCountDisplay => StudentsCount.ToString("N0");
        public string CoursesCountDisplay => CoursesCount.ToString("N0");
        public string ProgramsCountDisplay => ProgramsCount.ToString("N0");

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }

        // Refresh command.
        public RelayCommand RefreshDashboardCommand { get; }

        // DispatcherTimer for periodic data refresh.
        private DispatcherTimer _refreshTimer;

        public DashboardViewModel(IRepository<Student> studentRepo, IRepository<Subject> subjectRepo, IRepository<AcademicProgram> programRepo)
        {
            _studentRepository = studentRepo;
            _subjectRepository = subjectRepo;
            _programRepository = programRepo;

            // Initialize chart data with dummy values; replace with real data as needed.
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Students",
                    Values = new ChartValues<double> { 50, 60, 70, 80, 90, 100 },
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.DodgerBlue,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 10
                },
                new LineSeries
                {
                    Title = "Programs",
                    Values = new ChartValues<double> { 20, 30, 25, 40, 50, 55 },
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Green,
                    PointGeometry = DefaultGeometries.Square,
                    PointGeometrySize = 10
                },
                new LineSeries
                {
                    Title = "Subjects",
                    Values = new ChartValues<double> { 35, 40, 45, 50, 55, 60 },
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Red,
                    PointGeometry = DefaultGeometries.Triangle,
                    PointGeometrySize = 10
                }
            };

            Labels = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
            Formatter = value => value.ToString("N");

            // Initialize the refresh command.
            RefreshDashboardCommand = new RelayCommand(async _ => await RefreshDashboard());

            // Load initial data.
            _ = RefreshDashboard();

            // Set up a timer to automatically refresh data every 30 seconds.
            SetupRefreshTimer();
        }

        private void SetupRefreshTimer()
        {
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _refreshTimer.Tick += async (sender, args) =>
            {
                try
                {
                    await RefreshDashboard();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-refresh error: {ex.Message}");
                }
            };
            _refreshTimer.Start();
        }

        public async Task RefreshDashboard()
        {
            try
            {
                // Fetch counts concurrently
                var studentCountTask = _studentRepository.GetCountAsync();
                var courseCountTask = _subjectRepository.GetCountAsync();
                var programCountTask = _programRepository.GetCountAsync();

                await Task.WhenAll(studentCountTask, courseCountTask, programCountTask);

                // Update properties using the completed tasks' results
                StudentsCount = studentCountTask.Result;
                CoursesCount = courseCountTask.Result;
                ProgramsCount = programCountTask.Result;
            }
            catch (Exception ex)
            {
                // Log detailed error
                System.Diagnostics.Debug.WriteLine($"Refresh error: {ex}");
                // Consider showing an error message to the user
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
