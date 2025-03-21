using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
        // Private backing fields
        private string _searchText;
        private ObservableCollection<Grade> _grades;
        private ObservableCollection<string> _semesters;
        private string _selectedSemester;
        private ObservableCollection<string> _years;
        private string _selectedYear;
        private ObservableCollection<string> _programs;
        private string _selectedProgram;
        private bool _isLoading;

        // Notifier for user feedback
        private readonly Notifier _notifier;

        // Validation dictionary for INotifyDataErrorInfo
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
                    // Trigger filtering asynchronously
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

        #endregion

        #region Commands

        public ICommand DeleteGradeCommand { get; }
        public ICommand UploadFileCommand { get; }
        public ICommand InputGradeCommand { get; }
        public ICommand AddSubjectCommand { get; }
        public ICommand EnterGradeCommand { get; }

        #endregion

        #region Constructor

        public GradeDashboardViewModel()
        {
            // Initialize notifier with ToastNotifications configuration
            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(corner: Corner.BottomRight, offsetX: 10, offsetY: 10);
                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));
                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            // Load filter options from the database with error handling
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    Semesters = new ObservableCollection<string>(context.Students
                        .Select(s => s.Semester)
                        .Distinct()
                        .ToList());

                    Years = new ObservableCollection<string>(context.Students
                        .Select(s => s.Year)
                        .Distinct()
                        .ToList());

                    Programs = new ObservableCollection<string>(context.Students
                        .Select(s => s.AcademicProgram.ProgramCode)
                        .Distinct()
                        .ToList());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading filter options: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Initialize commands with parameter validation
            DeleteGradeCommand = new RelayCommand(DeleteGrade, param => param is Grade);
            UploadFileCommand = new RelayCommand(UploadGrades);
            InputGradeCommand = new RelayCommand(InputGrade, param => param is Grade);
            AddSubjectCommand = new RelayCommand(AddSubject);
            EnterGradeCommand = new RelayCommand(EnterGrade);

            // Load initial grades asynchronously
            _ = LoadGradesAsync();
        }

        #endregion

        #region Data Loading & Filtering

        private async Task LoadGradesAsync()
        {
            try
            {
                IsLoading = true;
                using (var context = new ApplicationDbContext())
                {
                    var grades = await context.Grade
                        .Include(g => g.Student)
                        .Include(g => g.Subject)
                        .ToListAsync();

                    // Update Grades on the UI thread
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
                        .Include(g => g.Subject)
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

        private void InputGrade(object parameter)
        {
            if (parameter is Grade selectedGrade)
            {
                try
                {
                    var inputGradeWindow = new InputGrade();
                    inputGradeWindow.DataContext = new InputGradeViewModel(selectedGrade);
                    inputGradeWindow.ShowDialog();
                    // Refresh data after editing
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

        // Sample validation: ensure the search text is not too long
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
