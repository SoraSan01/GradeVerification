using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public ICommand ExportToPdfCommand { get; }

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
            ExportToPdfCommand = new RelayCommand(ExportToPdf);

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
                    var grades = await context.Grade
                        .Include(g => g.Student)
                            .ThenInclude(s => s.AcademicProgram) // Add this
                        .Include(g => g.Subject)
                        .AsNoTracking() // Add this to prevent context tracking
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

        private void ExportToPdf(object parameter)
        {
            try
            {
                // Get the currently filtered grades
                if (Grades.Count == 0)
                {
                    MessageBox.Show("No grades to export", "Export Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Ask user to specify the location to save the PDF file
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    FileName = "Grade_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Group grades by student to create one report per student
                    var studentGroups = Grades.GroupBy(g => g.StudentId).ToList();

                    // Create a document
                    PdfDocument document = new PdfDocument();

                    // Get the current date for the report
                    var currentDate = DateTime.Now.ToString("MMMM dd, yyyy");

                    foreach (var studentGroup in studentGroups)
                    {
                        var student = studentGroup.First().Student;

                        // Get the first grade to extract student information
                        var firstGrade = studentGroup.First();

                        // Add a page
                        PdfPage page = document.AddPage();
                        XGraphics gfx = XGraphics.FromPdfPage(page);

                        // Define fonts
                        XFont headerFont = new XFont("Arial", 18);
                        XFont titleFont = new XFont("Arial", 14);
                        XFont normalFont = new XFont("Arial", 12);
                        XFont smallFont = new XFont("Arial", 10);
                        XFont boldFont = new XFont("Arial", 12);

                        // Draw the header
                        gfx.DrawString("Student Grade Report", headerFont, XBrushes.DarkBlue,
                            new XRect(0, 30, page.Width, 30), XStringFormats.Center);

                        // Draw the date
                        gfx.DrawString($"Date: {currentDate}", normalFont, XBrushes.Black,
                            new XRect(page.Width - 200, 30, 150, 20), XStringFormats.CenterRight);

                        // Draw student information
                        int yPos = 80;
                        int indent = 40;

                        gfx.DrawString("Student Information:", titleFont, XBrushes.DarkBlue, indent, yPos);
                        yPos += 25;

                        gfx.DrawString($"Name: {student.FullName}", normalFont, XBrushes.Black, indent, yPos);
                        yPos += 20;

                        gfx.DrawString($"School ID: {student.SchoolId}", normalFont, XBrushes.Black, indent, yPos);
                        yPos += 20;

                        gfx.DrawString($"Program: {student.AcademicProgram?.ProgramName ?? "N/A"}", normalFont, XBrushes.Black, indent, yPos);
                        yPos += 20;

                        gfx.DrawString($"Year Level: {student.Year}", normalFont, XBrushes.Black, indent, yPos);
                        yPos += 20;

                        gfx.DrawString($"Semester: {student.Semester}", normalFont, XBrushes.Black, indent, yPos);
                        yPos += 20;

                        gfx.DrawString($"School Year: {student.SchoolYear}", normalFont, XBrushes.Black, indent, yPos);
                        yPos += 30;

                        // Draw grades table header
                        gfx.DrawString("Subject Grades:", titleFont, XBrushes.DarkBlue, indent, yPos);
                        yPos += 25;

                        // Table column positions
                        int[] columnPositions = new int[] { indent, indent + 120, indent + 270, indent + 370 };
                        int rowHeight = 25;

                        // Draw table header
                        gfx.DrawString("Subject Code", boldFont, XBrushes.Black, columnPositions[0], yPos);
                        gfx.DrawString("Professor", boldFont, XBrushes.Black, columnPositions[1], yPos);
                        gfx.DrawString("Schedule", boldFont, XBrushes.Black, columnPositions[2], yPos);
                        gfx.DrawString("Grade", boldFont, XBrushes.Black, columnPositions[3], yPos);

                        yPos += rowHeight;

                        // Draw horizontal line under header
                        gfx.DrawLine(new XPen(XColors.Gray, 1),
                            new XPoint(indent, yPos - 5),
                            new XPoint(columnPositions[3] + 50, yPos - 5));

                        // Draw rows for each subject
                        foreach (var grade in studentGroup)
                        {
                            gfx.DrawString(grade.Subject?.SubjectCode ?? "-", smallFont, XBrushes.Black, columnPositions[0], yPos);
                            gfx.DrawString(grade.Subject?.Professor ?? "-", smallFont, XBrushes.Black, columnPositions[1], yPos);
                            gfx.DrawString(grade.Subject?.Schedule ?? "-", smallFont, XBrushes.Black, columnPositions[2], yPos);

                            // Determine color based on grade value
                            XBrush gradeBrush = XBrushes.Black;
                            if (grade.IsGradeLow)
                            {
                                gradeBrush = XBrushes.Red;
                            }

                            gfx.DrawString(grade.Score ?? "-", boldFont, gradeBrush, columnPositions[3], yPos);

                            yPos += rowHeight;

                            // Check if we need a new page
                            if (yPos > page.Height - 50)
                            {
                                page = document.AddPage();
                                gfx = XGraphics.FromPdfPage(page);
                                yPos = 50;
                            }
                        }

                        // Draw horizontal line at bottom of table
                        gfx.DrawLine(new XPen(XColors.Gray, 1),
                            new XPoint(indent, yPos - 5),
                            new XPoint(columnPositions[3] + 50, yPos - 5));

                        // Add some notes at the bottom
                        yPos += 20;
                        gfx.DrawString("Notes:", boldFont, XBrushes.Black, indent, yPos);
                        yPos += 20;
                        gfx.DrawString("This is an unofficial grade report. Please contact the registrar for official transcripts.",
                            smallFont, XBrushes.Black, indent, yPos);
                    }

                    // Enable Unicode support
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                    // Save the document
                    document.Save(saveFileDialog.FileName);

                    // Notify the user
                    _notifier.ShowSuccess("PDF exported successfully!");

                    // Ask if user wants to open the PDF
                    var openResult = MessageBox.Show("PDF has been created successfully. Do you want to open it now?",
                        "Export Successful", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (openResult == MessageBoxResult.Yes)
                    {
                        // Open the file with the default PDF viewer
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to PDF: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
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