using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Org.BouncyCastle.Asn1.Pkcs;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class GradeDashboardViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        // Backing fields
        private string _searchText;
        private string _selectedSemester;
        private string _selectedYear;
        private string _selectedProgram;
        private bool _isLoading;
        private Grade _selectedGrade;

        private ObservableCollection<Grade> _grades = new ObservableCollection<Grade>();
        private ObservableCollection<string> _semesters = new ObservableCollection<string>();
        private ObservableCollection<string> _programs = new ObservableCollection<string>();
        private ObservableCollection<string> _years = new ObservableCollection<string>();

        // Notifier for user feedback
        private readonly Notifier _notifier;

        // Validation errors
        private readonly Dictionary<string, List<string>> _propertyErrors = new Dictionary<string, List<string>>();

        #region Properties

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    ValidateSearchText();
                    _ = FilterGradesAsync();
                }
            }
        }

        public ObservableCollection<Grade> Grades
        {
            get => _grades;
            set { _grades = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Semesters
        {
            get => _semesters;
            set { _semesters = value; OnPropertyChanged(); }
        }

        public string SelectedSemester
        {
            get => _selectedSemester;
            set
            {
                if (_selectedSemester != value)
                {
                    _selectedSemester = value;
                    OnPropertyChanged();
                    _ = FilterGradesAsync();
                }
            }
        }

        public ObservableCollection<string> Years
        {
            get => _years;
            set { _years = value; OnPropertyChanged(); }
        }

        public string SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear != value)
                {
                    _selectedYear = value;
                    OnPropertyChanged();
                    _ = FilterGradesAsync();
                }
            }
        }

        public ObservableCollection<string> Programs
        {
            get => _programs;
            set { _programs = value; OnPropertyChanged(); }
        }

        public string SelectedProgram
        {
            get => _selectedProgram;
            set
            {
                if (_selectedProgram != value)
                {
                    _selectedProgram = value;
                    OnPropertyChanged();
                    _ = FilterGradesAsync();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public Grade SelectedGrade
        {
            get => _selectedGrade;
            set
            {
                if (_selectedGrade != value)
                {
                    _selectedGrade = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand DeleteGradeCommand { get; }
        public ICommand UploadFileCommand { get; }
        public ICommand InputGradeCommand { get; }
        public ICommand AddSubjectCommand { get; }
        public ICommand EnterGradeCommand { get; }
        public ICommand AddCompletionGradeCommand { get; set; }
        #endregion

        #region Constructor

        public GradeDashboardViewModel()
        {
            // Initialize notifier with ToastNotifications configuration.
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

            // Initialize commands with parameter validation.
            DeleteGradeCommand = new RelayCommand(DeleteGrade, param => param is Grade);
            UploadFileCommand = new RelayCommand(UploadGrades);
            InputGradeCommand = new RelayCommand(InputGrade, param => param is Grade);
            AddSubjectCommand = new RelayCommand(AddSubject);
            EnterGradeCommand = new RelayCommand(EnterGrade);
            AddCompletionGradeCommand = new RelayCommand(AddCompletionGrade, CanAddCompletionGrade);

            // Load initial data.
            _ = LoadFilterOptionsAsync();
            _ = LoadGradesAsync();
        }

        #endregion

        #region Data Loading & Filtering

        private async Task LoadFilterOptionsAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    // Load distinct filter options from the Students table.
                    var semesters = await context.Students.Select(s => s.Semester).Distinct().ToListAsync();
                    var years = await context.Students.Select(s => s.Year).Distinct().ToListAsync();
                    var programs = await context.Students.Select(s => s.AcademicProgram.ProgramCode).Distinct().ToListAsync();

                    // If you're not on the UI thread, ensure the update happens on the UI thread:
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Semesters = new ObservableCollection<string>(semesters);
                        Years = new ObservableCollection<string>(years);
                        Programs = new ObservableCollection<string>(programs);
                    });
                }
            }
            catch (Exception ex)
            {
                // For production, consider logging this instead of using MessageBox.
                MessageBox.Show($"Error loading filter options: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task LoadGradesAsync()
        {
            try
            {
                IsLoading = true;
                using (var context = new ApplicationDbContext())
                {
                    // Disable change tracking for better performance
                    context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                    var grades = await context.Grade
                        .Include(g => g.Student)
                        .ThenInclude(s => s.AcademicProgram)
                        .Include(g => g.Subject)
                        .ToListAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Grades = new ObservableCollection<Grade>(grades);
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading grades: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task FilterGradesAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var query = context.Grade
                    .Include(g => g.Student)
                    .ThenInclude(s => s.AcademicProgram) // Add this
                    .Include(g => g.Subject)
                    .AsNoTracking() // Add this
                    .AsQueryable();

                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        var searchLower = SearchText.ToLower();
                        query = query.Where(g => (g.Student.FirstName + " " + g.Student.LastName).ToLower().Contains(searchLower) ||
                                                 g.Student.SchoolId.ToLower().Contains(searchLower) ||
                                                 g.Subject.SubjectCode.ToLower().Contains(searchLower));
                    }

                    if (!string.IsNullOrWhiteSpace(SelectedSemester))
                        query = query.Where(g => g.Subject.Semester.ToLower() == SelectedSemester.ToLower());

                    if (!string.IsNullOrWhiteSpace(SelectedYear))
                        query = query.Where(g => g.Student.Year.ToLower() == SelectedYear.ToLower());

                    if (!string.IsNullOrWhiteSpace(SelectedProgram))
                        query = query.Where(g => g.Student.AcademicProgram.ProgramCode.ToLower() == SelectedProgram.ToLower());

                    var filteredGrades = await query.ToListAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Grades = new ObservableCollection<Grade>(filteredGrades);
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering grades: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Command Handlers

        private async void DeleteGrade(object parameter)
        {
            if (parameter is Grade selectedGrade)
            {
                var result = MessageBox.Show("Are you sure you want to delete this grade?", "Confirm Delete",
                                             MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new ApplicationDbContext())
                        {
                            context.Grade.Remove(selectedGrade);
                            await context.SaveChangesAsync();
                        }
                        await LoadGradesAsync();
                        _notifier.ShowSuccess("Grade deleted successfully.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting grade: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void InputGrade(object parameter)
        {
            if (parameter is Grade selectedGrade)
            {
                try
                {
                    var inputGradeWindow = new InputGrade();
                    inputGradeWindow.DataContext = new InputGradeViewModel(selectedGrade);
                    inputGradeWindow.ShowDialog();
                    // Refresh grades after editing.
                    _ = LoadGradesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading grade for editing: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a grade to edit.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddCompletionGrade(object parameter)
        {
            if (parameter is Grade selectedGrade)
            {
                try
                {
                    var dialog = new CompletionGradeDialog();
                    var vm = new CompletionGradeDialogViewModel(selectedGrade);
                    dialog.DataContext = vm;

                    if (dialog.ShowDialog() == true)
                    {
                        _notifier.ShowSuccess("Completion grade added successfully");
                        _ = LoadGradesAsync(); // Refresh the list
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding completion grade: {ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanAddCompletionGrade(object parameter)
        {
            var grade = parameter as Grade;
            return grade != null && grade.CompletionEligible; // Check if the student is eligible for a completion grade
        }

        private void EnterGrade(object parameter)
        {
            var enterGradeWindow = new EnterGrade();
            enterGradeWindow.DataContext = new EnterGradeViewModel();
            enterGradeWindow.ShowDialog();
        }

        private void UploadGrades(object parameter)
        {
            var uploadGradesWindow = new UploadGrades();
            uploadGradesWindow.DataContext = new UploadGradesViewModel();
            uploadGradesWindow.ShowDialog();
        }

        private void AddSubject(object parameter)
        {
            try
            {
                var selectStudentWindow = new SelectStudent();
                selectStudentWindow.DataContext = new SelectStudentViewModel();
                selectStudentWindow.ShowDialog();
                _ = LoadGradesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Validation (INotifyDataErrorInfo)

        public bool HasErrors => _propertyErrors.Any();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;
            return _propertyErrors.ContainsKey(propertyName) ? _propertyErrors[propertyName] : null;
        }

        private void AddError(string propertyName, string error)
        {
            if (!_propertyErrors.ContainsKey(propertyName))
                _propertyErrors[propertyName] = new List<string>();

            if (!_propertyErrors[propertyName].Contains(error))
            {
                _propertyErrors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_propertyErrors.ContainsKey(propertyName))
            {
                _propertyErrors.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        private void ValidateSearchText()
        {
            ClearErrors(nameof(SearchText));
            if (!string.IsNullOrEmpty(SearchText) && SearchText.Length > 100)
            {
                AddError(nameof(SearchText), "Search text cannot exceed 100 characters.");
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}