using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Drawing;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using PdfSharp.Pdf;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;
using Microsoft.Win32;
using PdfSharp;
using OpenXmlPowerTools;

namespace GradeVerification.ViewModel
{
    public class StudentDashboardViewModel : INotifyPropertyChanged
    {
        private readonly Notifier _notifier;
        private readonly DispatcherTimer _filterTimer;

        public ObservableCollection<Student> Students { get; set; } = new ObservableCollection<Student>();
        private ObservableCollection<Student> _allStudents = new ObservableCollection<Student>();

        public ObservableCollection<string> Semesters { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Years { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Programs { get; set; } = new ObservableCollection<string>();
        // New SchoolYears collection.
        public ObservableCollection<string> SchoolYears { get; set; } = new ObservableCollection<string>();

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ResetFilterTimer(); }
        }

        private string _selectedSemester;
        public string SelectedSemester
        {
            get => _selectedSemester;
            set { _selectedSemester = value; OnPropertyChanged(); ResetFilterTimer(); }
        }

        private string _selectedYear;
        public string SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(); ResetFilterTimer(); }
        }

        // New SelectedSchoolYear property.
        private string _selectedSchoolYear;
        public string SelectedSchoolYear
        {
            get => _selectedSchoolYear;
            set { _selectedSchoolYear = value; OnPropertyChanged(); ResetFilterTimer(); }
        }

        private string _selectedProgram;
        public string SelectedProgram
        {
            get => _selectedProgram;
            set { _selectedProgram = value; OnPropertyChanged(); ResetFilterTimer(); }
        }

        private bool _showDeletedStudents;
        public bool ShowDeletedStudents
        {
            get => _showDeletedStudents;
            set
            {
                _showDeletedStudents = value;
                OnPropertyChanged();
                FilterStudents();
            }
        }

        // Commands for various actions.
        public ICommand AddStudentCommand { get; }
        public ICommand EditStudentCommand { get; }
        public ICommand DeleteStudentCommand { get; }
        public ICommand ShowGradeCommand { get; }
        public ICommand UploadStudentCommand { get; }
        public ICommand RestoreStudentCommand { get; }
        public ICommand ExportGradeCommand { get; }

        public StudentDashboardViewModel()
        {
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

            // Initialize and configure the filter debounce timer.
            _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _filterTimer.Tick += FilterTimer_Tick;

            AddStudentCommand = new RelayCommand(AddStudent);
            EditStudentCommand = new RelayCommand(EditStudent, CanModifyStudent);
            DeleteStudentCommand = new RelayCommand(async param => await DeleteStudent(param), CanModifyStudent);
            RestoreStudentCommand = new RelayCommand(async param => await RestoreStudent(param), CanRestoreStudent);
            ShowGradeCommand = new RelayCommand(ShowGrade, CanModifyStudent);
            UploadStudentCommand = new RelayCommand(UploadWindow);
            ExportGradeCommand = new RelayCommand(ExportGradeToPdf, CanModifyStudent);

            // Load students initially.
            LoadStudentsAsync();
        }

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
        private void FilterTimer_Tick(object sender, EventArgs e)
        {
            _filterTimer.Stop();
            FilterStudents();
        }

        /// <summary>
        /// Loads all students asynchronously and updates the master and filtered collections.
        /// Also populates the Programs and SchoolYears collections.
        /// </summary>
        private async void LoadStudentsAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var studentList = await context.Students
                        .Include(s => s.AcademicProgram)
                        .ToListAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _allStudents.Clear();
                        Programs.Clear();
                        SchoolYears.Clear();
                        Years.Clear();
                        Semesters.Clear();

                        foreach (var student in studentList)
                        {
                            _allStudents.Add(student); // Add to master list

                            // Populate filter options
                            if (!Years.Contains(student.Year))
                                Years.Add(student.Year);
                            if (!Semesters.Contains(student.Semester))
                                Semesters.Add(student.Semester);
                            if (!Programs.Contains(student.AcademicProgram.ProgramCode))
                                Programs.Add(student.AcademicProgram.ProgramCode);
                            if (!string.IsNullOrWhiteSpace(student.SchoolYear) && !SchoolYears.Contains(student.SchoolYear))
                                SchoolYears.Add(student.SchoolYear);
                        }

                        FilterStudents(); // Apply initial filter
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading students: {ex.Message}");
                _notifier.ShowError("Error loading students.");
            }
        }

        /// <summary>
        /// Filters the student list based on the search text and filter selections.
        /// </summary>
        private void FilterStudents()
        {
            var filtered = _allStudents.Where(student =>
                (string.IsNullOrWhiteSpace(SearchText) ||
                 student.FullName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 student.SchoolId.Contains(SearchText)) &&
                (string.IsNullOrWhiteSpace(SelectedSemester) ||
                 student.Semester.Equals(SelectedSemester, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(SelectedYear) ||
                 student.Year.Equals(SelectedYear, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(SelectedSchoolYear) ||
                 student.SchoolYear.Equals(SelectedSchoolYear, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(SelectedProgram) ||
                 student.AcademicProgram.ProgramCode.Equals(SelectedProgram, StringComparison.OrdinalIgnoreCase)) &&
                (student.IsDeleted == false || ShowDeletedStudents) // Explicitly handle deletion status
            ).ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Students.Clear();
                foreach (var student in filtered)
                {
                    Students.Add(student);
                }
            });
        }

        private void AddStudent(object parameter)
        {
            var addStudentWindow = new AddStudent
            {
                DataContext = new AddStudentViewModel(LoadStudentsAsync)
            };

            if (addStudentWindow.ShowDialog() == true)
            {
                LoadStudentsAsync();
            }
        }

        private void EditStudent(object parameter)
        {
            if (parameter is Student studentToEdit)
            {
                var editWindow = new EditStudent(); // Create the edit window.
                editWindow.DataContext = new EditStudentViewModel(studentToEdit, editWindow, LoadStudentsAsync);
                editWindow.Show();
            }
        }

        private async Task DeleteStudent(object parameter)
        {
            if (parameter is Student studentToDelete)
            {
                var result = MessageBox.Show($"Are you sure you want to delete {studentToDelete.FullName}?",
                    "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new ApplicationDbContext())
                        {
                            // Attach the student if not already tracked
                            context.Students.Attach(studentToDelete);
                            studentToDelete.IsDeleted = true;
                            await context.SaveChangesAsync();
                            LoadStudentsAsync();
                        }
                        ShowSuccessNotification("Student deleted successfully!");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting student: {ex.Message}");
                        ShowErrorNotification("Error deleting student.");
                    }
                }
            }
        }


        private async Task RestoreStudent(object parameter)
        {
            if (parameter is Student studentToRestore)
            {
                var result = MessageBox.Show($"Are you sure you want to restore {studentToRestore.FullName}?",
                    "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new ApplicationDbContext())
                        {
                            // Attach the student if not already tracked
                            context.Students.Attach(studentToRestore);
                            studentToRestore.IsDeleted = false;
                            await context.SaveChangesAsync();
                            LoadStudentsAsync();
                        }
                        ShowSuccessNotification("Student restored successfully!");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error restoring student: {ex.Message}");
                        ShowErrorNotification("Error restoring student.");
                    }
                }
            }
        }


        private bool CanModifyStudent(object parameter) => parameter is Student;
        private bool CanRestoreStudent(object parameter)
        {
            return parameter is Student student && student.IsDeleted;
        }

        private void ShowGrade(object parameter)
        {
            if (parameter is Student student)
            {
                var showGradeWindow = new ShowGradeWindow
                {
                    DataContext = new ShowGradeViewModel(student)
                };
                showGradeWindow.ShowDialog();
            }
        }

        private void ExportGradeToPdf(object parameter)
        {
            if (parameter is Student student)
            {
                try
                {
                    var saveFileDialog = new SaveFileDialog
                    {
                        Filter = "PDF files (*.pdf)|*.pdf",
                        DefaultExt = "pdf",
                        FileName = $"{student.FullName}_Grade_Report_{DateTime.Now:yyyyMMdd_HHmmss}"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        using (var document = new PdfDocument())
                        {
                            // Load student grades
                            using (var context = new ApplicationDbContext())
                            {
                                var grades = context.Grade
                                    .Include(g => g.Subject)
                                    .Where(g => g.StudentId == student.Id)
                                    .ToList();

                                CreatePdfPage(document, student, grades);
                            }

                            document.Save(saveFileDialog.FileName);
                        }

                        Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
                        _notifier.ShowSuccess("PDF exported successfully!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting PDF: {ex.Message}", "Export Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreatePdfPage(PdfDocument document, Student student, List<Grade> grades)
        {
            // Set custom page size (5.5" x 8.5" - half letter size)
            var page = document.AddPage();
            page.Width = 396;  // 5.5 * 72 points
            page.Height = 612; // 8.5 * 72 points
            var gfx = XGraphics.FromPdfPage(page);

            // Define colors and fonts
            var darkGreen = XColor.FromArgb(255, 46, 125, 50);
            var mediumGreen = XColor.FromArgb(255, 56, 142, 60);
            var lightGreen = XColor.FromArgb(255, 241, 248, 233);
            var failGradeColor = XColor.FromArgb(255, 220, 53, 69); // Red color for grades below 75

            var headerFont = new XFont("Arial", 16, XFontStyleEx.Bold);
            var titleFont = new XFont("Arial", 12, XFontStyleEx.Bold);
            var normalFont = new XFont("Arial", 10);
            var smallFont = new XFont("Arial", 8);

            // University Header
            DrawUniversityHeader(gfx, page, document);

            // Main Title
            gfx.DrawString("STUDENT GRADE REPORT", titleFont, new XSolidBrush(darkGreen),
                new XRect(0, 90, page.Width, 20), XStringFormats.TopCenter);

            // Student Information
            DrawStudentInfo(gfx, page, student, mediumGreen, lightGreen);

            // Grades Table
            var finalY = DrawGradeTable(gfx, page, document, grades, mediumGreen, failGradeColor);

            // Signature Line
            DrawSignatureLine(gfx, page, finalY);

            // "Nothing Follows" now appears below the signature line
            DrawNothingFollows(gfx, page, finalY + 80);
        }

        private void DrawStudentInfo(XGraphics gfx, PdfPage page, Student student,
                       XColor labelColor, XColor bgColor)
        {
            var infoRect = new XRect(20, 120, page.Width - 40, 110);
            gfx.DrawRectangle(new XSolidBrush(bgColor), infoRect);

            double y = infoRect.Top + 10;
            double x = infoRect.Left + 10;
            double colWidth = (infoRect.Width - 20) / 2;

            // Left Column
            DrawInfoRow(gfx, "Name:", student.FullName, x, y, labelColor);
            DrawInfoRow(gfx, "ID:", student.SchoolId, x, y + 25, labelColor);
            DrawInfoRow(gfx, "Year:", student.Year, x, y + 50, labelColor);

            // Right Column
            DrawInfoRow(gfx, "Program:", student.AcademicProgram?.ProgramCode ?? "N/A", x + colWidth, y, labelColor);
            DrawInfoRow(gfx, "Semester:", student.Semester, x + colWidth, y + 25, labelColor);
            DrawInfoRow(gfx, "School Year:", student.SchoolYear, x + colWidth, y + 50, labelColor);
        }

        private void DrawInfoRow(XGraphics gfx, string label, string value,
                        double x, double y, XColor labelColor)
        {
            var boldFont = new XFont("Arial", 10, XFontStyleEx.Bold);
            var normalFont = new XFont("Arial", 10);

            gfx.DrawString(label, boldFont, new XSolidBrush(labelColor), x, y);
            gfx.DrawString(value, normalFont, XBrushes.Black, x + 60, y); // Increased spacing between label and value
        }

        private double DrawGradeTable(XGraphics gfx, PdfPage page, PdfDocument document,
              List<Grade> grades, XColor headerColor, XColor failGradeColor)
        {
            double[] columnWidths = { 100, 120, 100, 40 };
            double y = 240; // Increased y position to create more space after student info
            int rowHeight = 20;
            var currentPageNumber = 1;
            var headerFont = new XFont("Arial", 10, XFontStyleEx.Bold);
            string[] headers = { "Subject", "Schedule", "Professor", "Grade" };

            // Draw headers on first page
            DrawTableHeaders(gfx, page, headerColor, columnWidths, ref y, headers, headerFont);

            for (int i = 0; i < grades.Count; i++)
            {
                if (y + rowHeight > page.Height - 100)
                {
                    // Create new page
                    page = document.AddPage();
                    page.Width = 396;
                    page.Height = 612;
                    gfx = XGraphics.FromPdfPage(page);
                    DrawUniversityHeader(gfx, page, document);
                    y = 200;
                    currentPageNumber++;

                    // Draw headers on new page
                    DrawTableHeaders(gfx, page, headerColor, columnWidths, ref y, headers, headerFont);
                }

                var grade = grades[i];
                double x = 20;
                var rowFont = new XFont("Arial", 9);

                // Subject Code
                gfx.DrawString(grade.Subject?.SubjectCode ?? "-", rowFont, XBrushes.Black,
                    new XRect(x, y, columnWidths[0], rowHeight), XStringFormats.CenterLeft);
                x += columnWidths[0];

                // Schedule
                gfx.DrawString(grade.Subject?.Schedule ?? "-", rowFont, XBrushes.Black,
                    new XRect(x, y, columnWidths[1], rowHeight), XStringFormats.CenterLeft);
                x += columnWidths[1];

                // Professor
                gfx.DrawString(grade.Subject?.Professor ?? "-", rowFont, XBrushes.Black,
                    new XRect(x, y, columnWidths[2], rowHeight), XStringFormats.CenterLeft);
                x += columnWidths[2];

                // Handle completion grade
                var gradeValue = !string.IsNullOrEmpty(grade.CompletionGrade)
                    ? grade.CompletionGrade
                    : grade.Score ?? "-";

                // Check if grade is 74 or below for red font color
                bool isLowGrade = false;
                if (int.TryParse(gradeValue, out int numericGrade))
                {
                    isLowGrade = numericGrade <= 74;
                }
                else if (grade.IsGradeLow)
                {
                    isLowGrade = true;
                }

                var gradeBrush = isLowGrade ? new XSolidBrush(failGradeColor) : XBrushes.Black;

                gfx.DrawString(gradeValue, rowFont, gradeBrush,
                    new XRect(x, y, columnWidths[3], rowHeight), XStringFormats.CenterRight);

                y += rowHeight;
            }

            return y;
        }

        private void DrawTableHeaders(XGraphics gfx, PdfPage page, XColor headerColor,
            double[] columnWidths, ref double y, string[] headers, XFont headerFont)
        {
            double x = 20;

            for (int j = 0; j < headers.Length; j++)
            {
                var format = j == headers.Length - 1 ? XStringFormats.CenterRight : XStringFormats.CenterLeft;
                gfx.DrawString(headers[j], headerFont, new XSolidBrush(headerColor),
                    new XRect(x, y, columnWidths[j], 20), format);
                x += columnWidths[j];
            }

            y += 25;
            gfx.DrawLine(new XPen(XColors.Gray, 0.5), 20, y, page.Width - 20, y);
            y += 5;
        }

        private void DrawSignatureLine(XGraphics gfx, PdfPage page, double yPosition)
        {
            yPosition += 30; // Increased space before signature line
            gfx.DrawLine(new XPen(XColors.Black, 0.5),
                page.Width / 2 - 75, yPosition,
                page.Width / 2 + 75, yPosition);
            gfx.DrawString("Authorized Signature",
                new XFont("Arial", 8), XBrushes.Black,
                new XRect(page.Width / 2 - 75, yPosition + 5, 150, 15),
                XStringFormats.TopCenter);
        }

        private void DrawNothingFollows(XGraphics gfx, PdfPage page, double yPosition)
        {
            // Draw a clear box for proper centering
            var boxWidth = page.Width - 40;
            var boxHeight = 20;
            var boxX = 20;

            // Position the text in the center of the box with proper alignment formatting
            gfx.DrawString("- NOTHING FOLLOWS -",
                          new XFont("Arial", 8),
                          XBrushes.DarkGray,
                          new XRect(boxX, yPosition, boxWidth, boxHeight),
                          XStringFormats.Center); // Using Center alignment for proper positioning
        }

        private void DrawUniversityHeader(XGraphics gfx, PdfPage page, PdfDocument document)
        {
            var darkGreen = XColor.FromArgb(255, 46, 125, 50);
            var logo = XImage.FromFile("C:\\Users\\admin\\source\\repos\\GradeVerification\\GradeVerification\\Resources\\umlogo.png");

            // Header background
            gfx.DrawRectangle(new XSolidBrush(darkGreen), 0, 0, page.Width, 80);

            // Logo
            gfx.DrawImage(logo, new XRect(10, 10, 60, 60));

            // University name
            gfx.DrawString("University of Manila",
                new XFont("Arial", 14, XFontStyleEx.Bold),
                XBrushes.White,
                new XRect(80, 15, page.Width - 90, 20),
                XStringFormats.TopLeft);

            // Create multi-line string for better control over address line positioning
            var addressLine1 = "546 Mariano V. delos Santos Street, Sampaloc Manila";
            var addressLine2 = "Philippines 1008 | Tel: 8735-5085";
            var addressLine3 = "Email: umnla.edu.ph@gmail.com | Website: http://www.um.edu.ph";

            var addressFont = new XFont("Arial", 8);

            // Draw each line individually with proper spacing
            gfx.DrawString(addressLine1, addressFont, XBrushes.White, new XRect(80, 35, page.Width - 90, 12), XStringFormats.TopLeft);
            gfx.DrawString(addressLine2, addressFont, XBrushes.White, new XRect(80, 47, page.Width - 90, 12), XStringFormats.TopLeft);
            gfx.DrawString(addressLine3, addressFont, XBrushes.White, new XRect(80, 59, page.Width - 90, 12), XStringFormats.TopLeft);
        }
        private void UploadWindow(object parameter)
        {
            var uploadStudent = new UploadStudent();
            uploadStudent.DataContext = new UploadStudentViewModel();
            uploadStudent.Show();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
