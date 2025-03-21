using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
    public class EnterGradeViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationDbContext _context;
        private Notifier _notifier;
        private Student _selectedStudent;

        // Holds all grade entries (assigned subjects) for all student records with the given SchoolId.
        private List<GradeEntry> _allGradeEntries = new List<GradeEntry>();

        public EnterGradeViewModel()
        {
            _context = new ApplicationDbContext();
            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(corner: Corner.BottomRight, offsetX: 10, offsetY: 10);
                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(TimeSpan.FromSeconds(3), MaximumNotificationCount.FromCount(5));
                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            LoadSubjectsCommand = new RelayCommand(LoadGrades);
            SaveCommand = new RelayCommand(SaveGrades);
            CancelCommand = new RelayCommand(Cancel);
            DeleteSubjectCommand = new RelayCommand(DeleteSubject);
            FilteredSubjects = new ObservableCollection<GradeEntry>();

            // Options for filtering.
            SemesterOptions = new ObservableCollection<string> { "First Semester", "Second Semester", "Summer" };
            YearOptions = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
        }

        // Properties for binding and filtering.
        public string SchoolId { get; set; }
        public ObservableCollection<GradeEntry> FilteredSubjects { get; set; }

        // We'll store the first found student (as a default reference) but load grades from all student records.
        public Student SelectedStudent
        {
            get => _selectedStudent;
            set { _selectedStudent = value; OnPropertyChanged(nameof(SelectedStudent)); }
        }

        private string _selectedSemester;
        public string SelectedSemester
        {
            get => _selectedSemester;
            set
            {
                _selectedSemester = value;
                OnPropertyChanged(nameof(SelectedSemester));
                if (_allGradeEntries.Any())
                    FilterGradeEntries();
            }
        }

        private string _selectedYear;
        public string SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged(nameof(SelectedYear));
                if (_allGradeEntries.Any())
                    FilterGradeEntries();
            }
        }

        public ObservableCollection<string> SemesterOptions { get; }
        public ObservableCollection<string> YearOptions { get; }

        // Commands.
        public ICommand LoadSubjectsCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteSubjectCommand { get; }

        /// <summary>
        /// Loads all grade entries for all student records matching the SchoolId.
        /// </summary>
        private void LoadGrades(object parameter)
        {
            FilteredSubjects.Clear();
            _allGradeEntries.Clear();

            if (string.IsNullOrWhiteSpace(SchoolId))
            {
                _notifier.ShowError("Please enter a School ID.");
                return;
            }

            // Load all students with this SchoolId.
            var students = _context.Students
                .Include(s => s.Grades)
                    .ThenInclude(g => g.Subject)
                .Where(s => s.SchoolId == SchoolId)
                .ToList();

            if (!students.Any())
            {
                _notifier.ShowError("Student not found.");
                return;
            }

            // Use the first student as a reference (for default values).
            SelectedStudent = students.First();

            // Load all grade records for these students.
            var studentIds = students.Select(s => s.Id).ToList();
            var grades = _context.Grade
                .Include(g => g.Subject)
                .Include(g => g.Student)
                .Where(g => studentIds.Contains(g.StudentId))
                .ToList();

            // Convert each grade record into a GradeEntry.
            _allGradeEntries = grades.Select(g => new GradeEntry
            {
                GradeId = g.GradeId,
                StudentId = g.StudentId,
                SubjectId = g.SubjectId,
                SubjectCode = g.Subject.SubjectCode,
                SubjectName = g.Subject.SubjectName,
                GradeScore = g.Score,
                StudentYear = g.Student.Year,
                StudentSemester = g.Student.Semester
            }).ToList();

            // Initially, no filter is applied—display all grade entries.
            foreach (var entry in _allGradeEntries)
            {
                FilteredSubjects.Add(entry);
            }
        }

        /// <summary>
        /// Re-applies filters on the locally loaded grade entries.
        /// </summary>
        private void FilterGradeEntries()
        {
            FilteredSubjects.Clear();

            IEnumerable<GradeEntry> filtered = _allGradeEntries;

            if (!string.IsNullOrWhiteSpace(SelectedYear))
                filtered = filtered.Where(e => e.StudentYear.ToLower() == SelectedYear.ToLower());

            if (!string.IsNullOrWhiteSpace(SelectedSemester))
                filtered = filtered.Where(e => e.StudentSemester.ToLower() == SelectedSemester.ToLower());

            foreach (var entry in filtered)
                FilteredSubjects.Add(entry);
        }

        private void Cancel(object parameter)
        {
            Application.Current.Windows.OfType<EnterGrade>().FirstOrDefault()?.Close();
        }

        private void SaveGrades(object parameter)
        {
            // Validate each grade entry.
            foreach (var entry in FilteredSubjects)
            {
                string error = (entry as IDataErrorInfo)[nameof(GradeEntry.GradeScore)];
                if (!string.IsNullOrEmpty(error))
                {
                    _notifier.ShowError("Please correct invalid grade entries.");
                    return;
                }
            }

            // Update existing grade records or add new ones.
            foreach (var entry in FilteredSubjects)
            {
                var existingGrade = _context.Grade.FirstOrDefault(g => g.GradeId == entry.GradeId);
                if (existingGrade != null)
                {
                    existingGrade.Score = entry.GradeScore;
                }
                else
                {
                    var newGrade = new Grade
                    {
                        StudentId = entry.StudentId,
                        SubjectId = entry.SubjectId,
                        Score = entry.GradeScore
                    };
                    _context.Grade.Add(newGrade);
                }
            }

            _context.SaveChanges();
            _notifier.ShowSuccess("Grades saved successfully.");
        }

        /// <summary>
        /// Deletes the selected grade entry (subject) from the student’s subject list.
        /// </summary>
        private void DeleteSubject(object parameter)
        {
            if (parameter is GradeEntry entry)
            {
                // Optionally add confirmation dialog.
                if (MessageBox.Show($"Are you sure you want to delete subject '{entry.SubjectCode}' for the student?",
                                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                // Remove from the database if the grade record exists.
                var gradeRecord = _context.Grade.FirstOrDefault(g => g.GradeId == entry.GradeId);
                if (gradeRecord != null)
                {
                    _context.Grade.Remove(gradeRecord);
                }

                // Remove from local collections.
                _allGradeEntries.Remove(entry);
                FilteredSubjects.Remove(entry);

                _context.SaveChanges();
                _notifier.ShowSuccess("Subject deleted successfully.");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Helper class for grade entry with inline validation and extra properties for filtering.
    public class GradeEntry : INotifyPropertyChanged, IDataErrorInfo
    {
        private string _gradeScore;
        public string GradeId { get; set; }
        public string StudentId { get; set; }
        public string SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
        public string GradeScore
        {
            get => _gradeScore;
            set { _gradeScore = value; OnPropertyChanged(nameof(GradeScore)); }
        }
        // These properties are used for filtering by the student's registration info.
        public string StudentYear { get; set; }
        public string StudentSemester { get; set; }

        // List of allowed non-numeric grade codes.
        public List<string> AllowedGradeValues { get; } = new List<string> { "INC", "N/A", "NGS", "NN", "-", "DROP" };

        // IDataErrorInfo implementation for GradeScore.
        public string Error => null;
        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(GradeScore))
                {
                    // Allow an empty grade entry.
                    if (string.IsNullOrWhiteSpace(GradeScore))
                        return null;

                    // Check if the entered value is one of the allowed special values.
                    var allowedSpecialGrades = new[] { "INC", "N/A", "NGS", "NN", "-", "DROP" };
                    if (allowedSpecialGrades.Contains(GradeScore.Trim(), StringComparer.OrdinalIgnoreCase))
                        return null;

                    // Otherwise, validate that the input is numeric and between 0 and 100.
                    if (!double.TryParse(GradeScore, out double grade))
                        return "Grade must be numeric or one of: INC, N/A, NGS, NN, -, DROP.";
                    if (grade < 0 || grade > 100)
                        return "Grade must be between 0 and 100.";
                }
                return null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
