using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Presentation;
using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace GradeVerification.ViewModel
{
    public class SubjectDashboardViewModel : INotifyPropertyChanged
    {
        private string _searchText;
        private string _selectedYear;
        private string _selectedSemester;
        private ObservableCollection<Subject> _subjects;
        private readonly ApplicationDbContext _context;
        private readonly DispatcherTimer _filterTimer;

        public event PropertyChangedEventHandler PropertyChanged;

        public SubjectDashboardViewModel()
        {
            _context = new ApplicationDbContext();

            Subjects = new ObservableCollection<Subject>();
            YearOptions = new ObservableCollection<string>();
            SemesterOptions = new ObservableCollection<string>();

            // Initialize a debounce timer with a 300ms interval.
            _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _filterTimer.Tick += FilterTimer_Tick;

            // Initial load.
            LoadSubjectsAsync();

            AddSubjectCommand = new RelayCommand(AddSubject);
            EditSubjectCommand = new RelayCommand(EditSubject, CanModifySubject);
            DeleteSubjectCommand = new RelayCommand(DeleteSubject, CanModifySubject);
            BulkInsertCommand = new RelayCommand(BulkInsert);
        }

        public ObservableCollection<Subject> Subjects
        {
            get => _subjects;
            set
            {
                _subjects = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> YearOptions { get; }
        public ObservableCollection<string> SemesterOptions { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ResetFilterTimer();
            }
        }

        public string SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged();
                ResetFilterTimer();
            }
        }

        public string SelectedSemester
        {
            get => _selectedSemester;
            set
            {
                _selectedSemester = value;
                OnPropertyChanged();
                ResetFilterTimer();
            }
        }

        public ICommand AddSubjectCommand { get; }
        public ICommand EditSubjectCommand { get; }
        public ICommand DeleteSubjectCommand { get; }
        public ICommand BulkInsertCommand { get; }

        private async void LoadSubjectsAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var subjects = await context.Subjects
                                                .Include(s => s.AcademicProgram)
                                                .ToListAsync();

                    // Update the UI-bound collections asynchronously.
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        // Clear and update the Subjects collection.
                        Subjects.Clear();
                        foreach (var subject in subjects)
                        {
                            Subjects.Add(subject);
                        }

                        // Clear and update the filter collections.
                        YearOptions.Clear();
                        SemesterOptions.Clear();

                        foreach (var subject in subjects)
                        {
                            if (!string.IsNullOrWhiteSpace(subject.Year) && !YearOptions.Contains(subject.Year))
                            {
                                YearOptions.Add(subject.Year);
                            }
                            if (!string.IsNullOrWhiteSpace(subject.Semester) && !SemesterOptions.Contains(subject.Semester))
                            {
                                SemesterOptions.Add(subject.Semester);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading subjects: {ex.Message}");
            }
        }

        private void BulkInsert(object parameter)
        {
            var uploadSubject = new UploadSubject();
            uploadSubject.DataContext = new UploadSubjectViewModel(LoadSubjectsAsync);
            uploadSubject.Show();
        }

        private void AddSubject(object parameter)
        {
            try
            {
                var addSubjectWindow = new AddSubject
                {
                    DataContext = new AddSubjectViewModel(_context, LoadSubjectsAsync)
                };

                if (addSubjectWindow.ShowDialog() == true)
                {
                    LoadSubjectsAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding Subject: {ex.Message}");
            }
        }

        private void EditSubject(object parameter)
        {
            if (parameter is Subject selectedSubject)
            {
                var editWindow = new EditSubject(selectedSubject);
                var viewModel = new EditSubjectViewModel(selectedSubject, editWindow, LoadSubjectsAsync);
                editWindow.DataContext = viewModel;
                editWindow.ShowDialog();
            }
        }

        private void DeleteSubject(object parameter)
        {
            if (parameter is Subject subject)
            {
                _context.Subjects.Remove(subject);
                _context.SaveChanges();
                LoadSubjectsAsync();
            }
        }

        private bool CanModifySubject(object parameter) => parameter is Subject;

        /// <summary>
        /// Resets and starts the debounce timer.
        /// </summary>
        private void ResetFilterTimer()
        {
            _filterTimer.Stop();
            _filterTimer.Start();
        }

        /// <summary>
        /// Called when the debounce timer elapses.
        /// </summary>
        private async void FilterTimer_Tick(object sender, EventArgs e)
        {
            _filterTimer.Stop();
            await FilterSubjects();
        }

        /// <summary>
        /// Filters the subjects based on the search text and selected filters.
        /// </summary>
        private async Task FilterSubjects()
        {
            try
            {
                // Start with all subjects.
                var query = _context.Subjects
                    .Include(s => s.AcademicProgram)
                    .AsQueryable();

                // If search text is provided, do a case-insensitive search on SubjectName and SubjectCode.
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var lowerSearch = SearchText.Trim().ToLower();
                    query = query.Where(s => s.SubjectName.ToLower().Contains(lowerSearch) ||
                                             s.SubjectCode.ToLower().Contains(lowerSearch));
                }

                if (!string.IsNullOrWhiteSpace(SelectedYear))
                {
                    query = query.Where(s => s.Year == SelectedYear);
                }

                if (!string.IsNullOrWhiteSpace(SelectedSemester))
                {
                    query = query.Where(s => s.Semester == SelectedSemester);
                }

                var filteredSubjects = await query.ToListAsync();
                Subjects = new ObservableCollection<Subject>(filteredSubjects);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering subjects: {ex.Message}");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
