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
    public class SelectSubjectViewModel : INotifyPropertyChanged
    {
        private readonly Notifier _notifier;
        private readonly Student _selectedStudent;
        // Holds the complete list of subjects for the student's program.
        private List<Subject> _allSubjects = new List<Subject>();

        public ObservableCollection<Subject> Subjects { get; set; }
        public ObservableCollection<string> Programs { get; set; }
        public ObservableCollection<string> Years { get; set; }
        public ObservableCollection<string> Semesters { get; set; }

        public ICommand SaveSelectedSubjectsCommand { get; }
        public ICommand CancelCommand { get; }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterSubjects(); }
        }

        private string _selectedProgram;
        public string SelectedProgram
        {
            get => _selectedProgram;
            set { _selectedProgram = value; OnPropertyChanged(); FilterSubjects(); }
        }

        private string _selectedYear;
        public string SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(); FilterSubjects(); }
        }

        private string _selectedSemester;
        public string SelectedSemester
        {
            get => _selectedSemester;
            set { _selectedSemester = value; OnPropertyChanged(); FilterSubjects(); }
        }

        public SelectSubjectViewModel(Student student)
        {
            _selectedStudent = student ?? throw new ArgumentNullException(nameof(student));
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

            Subjects = new ObservableCollection<Subject>();
            Programs = new ObservableCollection<string>();
            Years = new ObservableCollection<string>();
            Semesters = new ObservableCollection<string>();

            SaveSelectedSubjectsCommand = new RelayCommand(SaveSelectedSubjects);
            CancelCommand = new RelayCommand(Cancel);

            LoadFilters();
            LoadSubjects();
        }

        /// <summary>
        /// Loads filter options (Programs, Years, Semesters) from subjects in the student's program.
        /// </summary>
        private void LoadFilters()
        {
            using (var context = new ApplicationDbContext())
            {
                Programs = new ObservableCollection<string>(
                    context.Subjects
                           .Where(s => s.ProgramId == _selectedStudent.ProgramId)
                           .Select(s => s.AcademicProgram.ProgramCode)
                           .Distinct()
                           .ToList());
                Years = new ObservableCollection<string>(
                    context.Subjects
                           .Where(s => s.ProgramId == _selectedStudent.ProgramId)
                           .Select(s => s.Year)
                           .Distinct()
                           .ToList());
                Semesters = new ObservableCollection<string>(
                    context.Subjects
                           .Where(s => s.ProgramId == _selectedStudent.ProgramId)
                           .Select(s => s.Semester)
                           .Distinct()
                           .ToList());
            }
        }

        /// <summary>
        /// Loads all subjects assigned to the student (i.e. those from the student's academic program).
        /// </summary>
        private void LoadSubjects()
        {
            using (var context = new ApplicationDbContext())
            {
                // Load only subjects for the student's academic program.
                var subjects = context.Subjects.AsNoTracking()
                    .Where(s => s.ProgramId == _selectedStudent.ProgramId)
                    .ToList();
                _allSubjects = subjects;

                // Mark subjects as selected if the student already has a Grade record for them.
                var assignedSubjectIds = context.Grade
                    .Where(g => g.StudentId == _selectedStudent.Id)
                    .Select(g => g.SubjectId)
                    .ToList();

                Subjects.Clear();
                foreach (var subject in _allSubjects)
                {
                    subject.IsSelected = assignedSubjectIds.Contains(subject.SubjectId);
                    Subjects.Add(subject);
                }
            }
        }

        /// <summary>
        /// Filters the locally loaded subjects (_allSubjects) based on SearchText and filter criteria.
        /// </summary>
        private void FilterSubjects()
        {
            if (_allSubjects == null || !_allSubjects.Any())
                return;

            var filtered = _allSubjects.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(s =>
                    s.SubjectName.IndexOf(SearchText, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    s.SubjectCode.IndexOf(SearchText, System.StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (!string.IsNullOrWhiteSpace(SelectedProgram))
            {
                filtered = filtered.Where(s => s.AcademicProgram != null &&
                    s.AcademicProgram.ProgramCode.Equals(SelectedProgram, System.StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(SelectedYear))
            {
                filtered = filtered.Where(s => s.Year.Equals(SelectedYear, System.StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(SelectedSemester))
            {
                filtered = filtered.Where(s => s.Semester.Equals(SelectedSemester, System.StringComparison.OrdinalIgnoreCase));
            }

            Subjects.Clear();
            foreach (var subject in filtered)
            {
                Subjects.Add(subject);
            }
        }

        /// <summary>
        /// Saves the selected subjects (those with IsSelected true) as Grade records for the student.
        /// Validates that at least one subject is selected.
        /// </summary>
        private void SaveSelectedSubjects(object parameter)
        {
            var selectedSubjects = Subjects.Where(s => s.IsSelected).ToList();
            if (!selectedSubjects.Any())
            {
                ShowErrorNotification("Please select at least one subject!");
                return;
            }

            using (var context = new ApplicationDbContext())
            {
                var existingSubjects = context.Grade
                    .Where(g => g.StudentId == _selectedStudent.Id)
                    .Select(g => g.SubjectId)
                    .ToList();

                foreach (var subject in selectedSubjects)
                {
                    if (!existingSubjects.Contains(subject.SubjectId))
                    {
                        var studentSubject = new Grade
                        {
                            StudentId = _selectedStudent.Id,
                            SubjectId = subject.SubjectId,
                            Score = null
                        };
                        context.Grade.Add(studentSubject);
                    }
                }
                context.SaveChanges();
            }
            ShowSuccessNotification("Subjects added successfully!");
            CloseWindow();
        }

        private void Cancel(object parameter)
        {
            // Removed erroneous "SelectSubject" usage.
            CloseWindow();
        }

        private void CloseWindow()
        {
            var window = Application.Current.Windows.OfType<SelectSubject>().FirstOrDefault();
            window?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
