using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
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
    public class SelectStudentViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Student _selectedStudent;
        private string _searchText;
        private Notifier _notifier;

        public Student SelectedStudent
        {
            get => _selectedStudent;
            set { _selectedStudent = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterStudents();
            }
        }

        // New filter properties
        private string _selectedYear;
        public string SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(); FilterStudents(); }
        }

        private string _selectedSemester;
        public string SelectedSemester
        {
            get => _selectedSemester;
            set { _selectedSemester = value; OnPropertyChanged(); FilterStudents(); }
        }

        private string _selectedProgram;
        public string SelectedProgram
        {
            get => _selectedProgram;
            set { _selectedProgram = value; OnPropertyChanged(); FilterStudents(); }
        }

        public ObservableCollection<Student> Students { get; set; }
        public ObservableCollection<Student> FilteredStudents { get; set; }

        // Collections for combo box filter options
        public ObservableCollection<string> YearOptions { get; set; }
        public ObservableCollection<string> SemesterOptions { get; set; }
        public ObservableCollection<string> ProgramOptions { get; set; }

        public ICommand SelectCommand { get; }
        public ICommand CancelCommand { get; }

        public SelectStudentViewModel()
        {
            Students = new ObservableCollection<Student>();
            FilteredStudents = new ObservableCollection<Student>();

            // Initialize filter options.
            YearOptions = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
            SemesterOptions = new ObservableCollection<string> { "First Semester", "Second Semester", "Summer" };
            ProgramOptions = new ObservableCollection<string>();

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

            SelectCommand = new RelayCommand(SelectStudent);
            CancelCommand = new RelayCommand(Cancel);

            LoadStudents();
        }

        private void LoadStudents()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    // Load only students who are either Non-Scholar or have a Summer semester.
                    var students = context.Students
                        .Where(s => s.Status == "Non-Scholar" || s.Semester == "Summer")
                        .ToList();

                    Students.Clear();
                    FilteredStudents.Clear();
                    foreach (var student in students)
                    {
                        Students.Add(student);
                        FilteredStudents.Add(student);
                    }

                    // Populate the ProgramOptions from the distinct programs among the loaded students.
                    ProgramOptions.Clear();
                    var programs = Students
                        .Select(s => s.AcademicProgram.ProgramCode) // Assumes Student has a Program property
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Distinct()
                        .OrderBy(p => p)
                        .ToList();
                    foreach (var program in programs)
                    {
                        ProgramOptions.Add(program);
                    }
                }
            }
            catch (Exception ex)
            {
                _notifier.ShowError($"Error loading students: {ex.Message}");
            }
        }

        private void FilterStudents()
        {
            FilteredStudents.Clear();
            var trimmedSearch = (SearchText ?? string.Empty).Trim();

            var filtered = Students.AsEnumerable();

            if (!string.IsNullOrEmpty(trimmedSearch))
            {
                filtered = filtered.Where(s =>
                    (!string.IsNullOrEmpty(s.FullName) && s.FullName.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.SchoolId) && s.SchoolId.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(SelectedYear))
            {
                filtered = filtered.Where(s => s.Year == SelectedYear);
            }

            if (!string.IsNullOrEmpty(SelectedSemester))
            {
                filtered = filtered.Where(s => s.Semester == SelectedSemester);
            }

            if (!string.IsNullOrEmpty(SelectedProgram))
            {
                filtered = filtered.Where(s => s.AcademicProgram.ProgramCode.Equals(SelectedProgram, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var student in filtered)
            {
                FilteredStudents.Add(student);
            }
        }

        private void SelectStudent(object parameter)
        {
            if (SelectedStudent == null)
            {
                _notifier.ShowError("Please select a student.");
                return;
            }

            // Open the SelectSubject window and pass the selected student.
            var selectSubjectWindow = new SelectSubject();
            selectSubjectWindow.DataContext = new SelectSubjectViewModel(SelectedStudent);
            selectSubjectWindow.ShowDialog();

            // Close the current window after subject selection.
            Application.Current.Windows.OfType<SelectStudent>().FirstOrDefault()?.Close();
        }

        private void Cancel(object parameter)
        {
            // Close the window without selecting a student.
            Application.Current.Windows.OfType<SelectStudent>().FirstOrDefault()?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Optional IDataErrorInfo implementation.
        public string Error => null;
        public string this[string columnName] => null;
    }
}
