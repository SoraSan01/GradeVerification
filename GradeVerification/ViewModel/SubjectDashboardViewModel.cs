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
        private readonly DispatcherTimer _filterTimer;

        private ObservableCollection<Subject> _subjects = new ObservableCollection<Subject>();

        public ObservableCollection<string> YearOptions { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> SemesterOptions { get; } = new ObservableCollection<string>();

        public ObservableCollection<Subject> Subjects
        {
            get => _subjects;
            set
            {
                _subjects = value;
                OnPropertyChanged();
            }
        }

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

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand AddSubjectCommand { get; }
        public ICommand EditSubjectCommand { get; }
        public ICommand DeleteSubjectCommand { get; }
        public ICommand BulkInsertCommand { get; }

        private const int PageSize = 100; // Adjust page size as needed

        public SubjectDashboardViewModel()
        {
            // Initialize debounce timer
            _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _filterTimer.Tick += FilterTimer_Tick;

            AddSubjectCommand = new RelayCommand(AddSubject);
            EditSubjectCommand = new RelayCommand(EditSubject, CanModifySubject);
            DeleteSubjectCommand = new RelayCommand(DeleteSubject, CanModifySubject);
            BulkInsertCommand = new RelayCommand(BulkInsert);

            // Initial load.
            LoadSubjectsAsync();
        }

        private async void LoadSubjectsAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    // Load only a limited number of subjects for initial display.
                    var subjects = await context.Subjects
                                                .Include(s => s.AcademicProgram)
                                                .Take(PageSize)
                                                .ToListAsync();

                    // Update UI-bound collections on the UI thread.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Subjects.Clear();
                        YearOptions.Clear();
                        SemesterOptions.Clear();

                        foreach (var subject in subjects)
                        {
                            Subjects.Add(subject);

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
                MessageBox.Show($"Error loading subjects: {ex.Message}");
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
                    DataContext = new AddSubjectViewModel(LoadSubjectsAsync)
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
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    if (parameter is Subject subject)
                    {
                        context.Subjects.Remove(subject);
                        context.SaveChanges();
                        LoadSubjectsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting Subject: {ex.Message}");
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
                using (var context = new ApplicationDbContext())
                {
                    // Start with all subjects.
                    var query = context.Subjects
                        .Include(s => s.AcademicProgram)
                        .AsQueryable();

                    // Filter by search text if provided.
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        var lowerSearch = SearchText.Trim().ToLower();
                        query = query.Where(s => s.SubjectName.ToLower().Contains(lowerSearch) ||
                                                 s.SubjectCode.ToLower().Contains(lowerSearch));
                    }
                    // Filter by selected year.
                    if (!string.IsNullOrWhiteSpace(SelectedYear))
                    {
                        query = query.Where(s => s.Year == SelectedYear);
                    }
                    // Filter by selected semester.
                    if (!string.IsNullOrWhiteSpace(SelectedSemester))
                    {
                        query = query.Where(s => s.Semester == SelectedSemester);
                    }

                    // Apply paging to limit the number of records loaded.
                    var filteredSubjects = await query
                                                .Take(PageSize)
                                                .ToListAsync();

                    // Update the Subjects collection on the UI thread.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Subjects.Clear();
                        foreach (var subject in filteredSubjects)
                        {
                            Subjects.Add(subject);
                        }
                    });
                }
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
