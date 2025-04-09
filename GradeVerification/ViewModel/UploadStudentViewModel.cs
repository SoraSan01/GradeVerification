using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Helper;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Win32;
using OfficeOpenXml;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    public class UploadStudentViewModel : INotifyPropertyChanged
    {
        private readonly Notifier _notifier;
        private string _filePath;

        public ObservableCollection<Student> StudentBulkCollection { get; private set; } = new();
        public ObservableCollection<SchoolYear> SchoolYears { get; set; } = new ObservableCollection<SchoolYear>();

        public ICommand BrowseCommand { get; }
        public ICommand UploadStudentCommand { get; }
        public ICommand CancelCommand { get; }

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged();
            }
        }

        private SchoolYear _selectedSchoolYear;
        public SchoolYear SelectedSchoolYear
        {
            get => _selectedSchoolYear;
            set
            {
                _selectedSchoolYear = value;
                OnPropertyChanged();
            }
        }

        public UploadStudentViewModel()
        {
            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(100));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });


            BrowseCommand = new RelayCommand(BrowseFile);
            UploadStudentCommand = new RelayCommand(async (_) => await SaveStudentsAsync());
            CancelCommand = new RelayCommand(Back);

            LoadSchoolYears();
        }

        private void BrowseFile(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xls;*.xlsx;*.xlsm",
                Title = "Select an Excel File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePath = openFileDialog.FileName;
                ExtractStudents(FilePath);
            }
        }

        private void Back(object parameter)
        {
            Application.Current.Windows.OfType<UploadStudent>().FirstOrDefault()?.Close();
        }

        private async void LoadSchoolYears()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var years = await context.SchoolYears
                        .OrderByDescending(y => y.SchoolYears)
                        .ToListAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SchoolYears.Clear();
                        foreach (var year in years)
                        {
                            // Assuming the model has a property named SchoolYears that is a string.
                            SchoolYears.Add(year);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading school years: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExtractStudents(string filepath)
        {
            try
            {
                if (!File.Exists(filepath))
                {
                    ShowErrorNotification("File not found.");
                    return;
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                using var package = new ExcelPackage(new FileInfo(filepath));
                StudentBulkCollection.Clear();

                using var context = new ApplicationDbContext();

                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    if (worksheet.Dimension == null)
                        continue;

                    // Parse program and semester from sheet name
                    var sheetName = worksheet.Name;
                    var splitIndex = sheetName.IndexOf(' ');
                    if (splitIndex == -1)
                    {
                        ShowErrorNotification($"Sheet name '{sheetName}' is invalid. Expected format: 'Program Semester'.");
                        continue;
                    }
                    var programCode = sheetName.Substring(0, splitIndex).Trim();
                    var semester = sheetName.Substring(splitIndex + 1).Trim();

                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    // Find the header row dynamically (where "NAME" appears)
                    int headerRow = -1;
                    for (int row = 1; row <= rowCount; row++)
                    {
                        var cellValue = worksheet.Cells[row, 1].Text?.Trim();
                        if (cellValue?.Equals("NAME", StringComparison.OrdinalIgnoreCase) ?? false)
                        {
                            headerRow = row;
                            break;
                        }
                    }

                    if (headerRow == -1)
                    {
                        ShowErrorNotification("Header row with 'NAME' not found.");
                        continue;
                    }

                    // Process data rows starting from the row after the header
                    for (int row = headerRow + 1; row <= rowCount; row++)
                    {
                        bool isRowEmpty = true;
                        for (int col = 1; col <= colCount; col++)
                        {
                            if (!string.IsNullOrWhiteSpace(worksheet.Cells[row, col].Text))
                            {
                                isRowEmpty = false;
                                break;
                            }
                        }
                        if (isRowEmpty)
                            continue;

                        var fullName = worksheet.Cells[row, 1].Text?.Trim();
                        var idnum = worksheet.Cells[row, 2].Text?.Trim();
                        var email = worksheet.Cells[row, 3].Text?.Trim();
                        var yearLevel = worksheet.Cells[row, 5].Text?.Trim(); // Column E (YR LEVEL)
                        var scholarship = worksheet.Cells[row, 6].Text?.Trim(); // Column F (SCHOLARSHIP)

                        yearLevel = InputNormalizer.NormalizeYear(yearLevel);
                        var normalizedSemester = InputNormalizer.NormalizeSemester(semester);

                        // Skip rows that accidentally match the header
                        if (fullName?.Equals("NAME", StringComparison.OrdinalIgnoreCase) ?? false)
                            continue;

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(fullName) ||
                            string.IsNullOrWhiteSpace(idnum) ||
                            string.IsNullOrWhiteSpace(email) ||
                            string.IsNullOrWhiteSpace(yearLevel))
                        {
                            ShowErrorNotification($"Missing required fields for student at row {row}. Skipping this row.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(scholarship))
                        {
                            ShowErrorNotification($"Missing scholarship field for student {idnum} at row {row}. Skipping this row.");
                            continue;
                        }

                        // Lookup academic program
                        var program = context.AcademicPrograms.AsNoTracking().FirstOrDefault(p => p.ProgramCode.ToLower() == programCode.ToLower().Trim());
                        if (program == null)
                        {
                            ShowErrorNotification($"Program {programCode} not found for student {idnum} at row {row}. Skipping this row.");
                            continue;
                        }

                        // Split full name
                        string lastName, firstName;
                        if (fullName.Contains(','))
                        {
                            var nameParts = fullName.Split(',', 2);
                            lastName = nameParts[0].Trim();
                            firstName = nameParts.Length > 1 ? nameParts[1].Trim() : "";
                        }
                        else
                        {
                            var tokens = fullName.Split(' ', 2);
                            firstName = tokens[0].Trim();
                            lastName = tokens.Length > 1 ? tokens[1].Trim() : "";
                        }

                        // Add student to collection
                        StudentBulkCollection.Add(new Student
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            SchoolId = idnum,
                            Email = email,
                            ProgramId = program.Id, // Foreign key (Id of AcademicProgram)
                            ProgramCode = program.ProgramCode, // Assign the display-friendly code
                            Year = yearLevel,
                            Status = scholarship,
                            Semester = normalizedSemester
                        });
                    }
                }

                ShowSuccessNotification("Data loaded successfully!");
            }
            catch (Exception ex)
            {
                ShowErrorNotification($"Error extracting students: {ex.Message}");
            }
        }

        private async Task SaveStudentsAsync()
        {
            if (StudentBulkCollection.Count == 0)
            {
                ShowErrorNotification("No data to save!");
                return;
            }

            // Validate school year selection
            if (string.IsNullOrEmpty(SelectedSchoolYear.SchoolYears))
            {
                ShowErrorNotification("Please select a school year!");
                return;
            }

            try
            {
                using var context = new ApplicationDbContext();
                using var transaction = await context.Database.BeginTransactionAsync();

                // This list will store students that were successfully added or updated.
                var successfullyProcessed = new List<Student>();

                foreach (var student in StudentBulkCollection)
                {
                    student.SchoolYear = SelectedSchoolYear.SchoolYears;

                    // Validate required fields.
                    if (string.IsNullOrEmpty(student.SchoolId) || string.IsNullOrEmpty(student.Email))
                    {
                        ShowErrorNotification($"Missing required fields for student {student.SchoolId}. Skipping this record.");
                        continue; // Skip this student
                    }

                    // Duplicate validation: Check if a student already exists with the same first name, last name, year, status, and semester.
                    bool duplicateExists = await context.Students
                        .AsNoTracking()
                        .AnyAsync(s =>
                            s.FirstName.ToLower() == student.FirstName.ToLower() &&
                            s.LastName.ToLower() == student.LastName.ToLower() &&
                            s.Year == student.Year &&
                            s.Status.ToLower() == student.Status.ToLower() &&
                            s.Semester.ToLower() == student.Semester.ToLower());

                    if (duplicateExists)
                    {
                        ShowErrorNotification(
                            $"Student {student.FirstName} {student.LastName} already exists in {student.Year}, {student.Status}, {student.Semester}. Skipping this record.");
                        continue; // Skip duplicate student
                    }

                    // Validate academic program.
                    var program = context.AcademicPrograms.AsNoTracking().FirstOrDefault(p => p.Id == student.ProgramId);
                    if (program == null)
                    {
                        ShowErrorNotification($"Invalid program for student {student.SchoolId}. Skipping this record.");
                        continue; // Skip if program is invalid
                    }

                    // Check if the student already exists by SchoolId.
                    var existingStudent = await context.Students.FirstOrDefaultAsync(s => s.SchoolId == student.SchoolId);
                    if (existingStudent == null)
                    {
                        // New student, add it.
                        context.Students.Add(student);
                    }
                    else
                    {
                        // Existing student, update its properties. 
                        // Avoid modifying key properties like StudentId.
                        context.Entry(existingStudent).CurrentValues.SetValues(student);
                    }

                    // Add to the successfully processed list, whether new or updated.
                    successfullyProcessed.Add(student);
                }

                // Save changes for student inserts/updates.
                await context.SaveChangesAsync();

                // Enroll scholars in subjects based on their program, year, and semester.
                foreach (var student in successfullyProcessed)
                {
                    if (student.Status.Equals("Scholar", StringComparison.OrdinalIgnoreCase))
                    {
                        var subjectsToEnroll = await context.Subjects
                            .Where(s => s.ProgramId == student.ProgramId && s.Year == student.Year && s.Semester == student.Semester)
                            .ToListAsync();

                        if (subjectsToEnroll.Count == 0)
                        {
                            ShowErrorNotification($"No subjects found for student {student.SchoolId}. Enrollment skipped for this student.");
                            continue; // Skip enrollment for this student
                        }

                        foreach (var subject in subjectsToEnroll)
                        {
                            var newGrade = new Grade
                            {
                                StudentId = student.Id,
                                SubjectId = subject.SubjectId,
                                ProfessorName = null,
                                Score = null
                            };

                            context.Grade.Add(newGrade);
                        }
                    }
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                ShowSuccessNotification("Students saved successfully!");
                StudentBulkCollection.Clear();
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ShowErrorNotification($"Error saving students: {errorMessage}");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ShowSuccessNotification(string message) => _notifier.ShowSuccess(message);
        private void ShowErrorNotification(string message) => _notifier.ShowError(message);
    }
}
