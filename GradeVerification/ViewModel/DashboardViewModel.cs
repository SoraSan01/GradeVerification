using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using GradeVerification.Repository;
using GradeVerification.Model;

namespace GradeVerification.ViewModel
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly IRepository<Student> _studentRepository;
        private readonly IRepository<Subject> _subjectRepository;
        private readonly IRepository<AcademicProgram> _programRepository;

        private string _studentsCount;
        private string _coursesCount;
        private string _programsCount;

        public string StudentsCount
        {
            get => _studentsCount;
            set { _studentsCount = value; OnPropertyChanged(); }
        }

        public string CoursesCount
        {
            get => _coursesCount;
            set { _coursesCount = value; OnPropertyChanged(); }
        }

        public string ProgramsCount
        {
            get => _programsCount;
            set { _programsCount = value; OnPropertyChanged(); }
        }

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }

        public DashboardViewModel(IRepository<Student> studentRepo, IRepository<Subject> subjectRepo, IRepository<AcademicProgram> programRepo)
        {
            _studentRepository = studentRepo;
            _subjectRepository = subjectRepo;
            _programRepository = programRepo;

            LoadDataAsync();

            // Initialize chart data with Students, Programs, and Subjects
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
        }

        private async void LoadDataAsync()
        {
            StudentsCount = (await _studentRepository.GetCountAsync()).ToString();
            CoursesCount = (await _subjectRepository.GetCountAsync()).ToString();
            ProgramsCount = (await _programRepository.GetCountAsync()).ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
