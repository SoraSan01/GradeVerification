using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
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

        public UploadStudentViewModel()
        {
            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(1.5),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(3));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            BrowseCommand = new RelayCommand(BrowseFile);
            UploadStudentCommand = new RelayCommand(async (_) => await SaveStudentsAsync());
            CancelCommand = new RelayCommand(Back);
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
                StudentBulkCollection.Clear(); // Clear previous data

                // Use a single context for lookups
                using var context = new ApplicationDbContext();

                // Process each worksheet in the workbook
                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    if (worksheet.Dimension == null)
                        continue;

                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    // Process from row 5 (assuming headers end before row 5)
                    for (int row = 5; row <= rowCount; row++)
                    {
                        // Check if the entire row is blank; if so, skip without error.
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

                        // Extract fields and trim any extra spaces.
                        var fullName = worksheet.Cells[row, 1].Text?.Trim();
                        var idnum = worksheet.Cells[row, 2].Text?.Trim();
                        var email = worksheet.Cells[row, 3].Text?.Trim();
                        var programCode = worksheet.Cells[row, 4].Text?.Trim();
                        var yearLevel = worksheet.Cells[row, 5].Text?.Trim();
                        var scholarship = worksheet.Cells[row, 6].Text?.Trim();
                        var semester = worksheet.Cells[row, 7].Text?.Trim();

                        // Skip header rows encountered within the data (if the first cell equals "Name")
                        if (!string.IsNullOrWhiteSpace(fullName) && fullName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Validate required fields (adjust as needed if certain fields are optional)
                        if (string.IsNullOrWhiteSpace(fullName) ||
                            string.IsNullOrWhiteSpace(idnum) ||
                            string.IsNullOrWhiteSpace(email) ||
                            string.IsNullOrWhiteSpace(programCode) ||
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

                        // Lookup academic program by program code
                        var program = context.AcademicPrograms.AsNoTracking().FirstOrDefault(p => p.ProgramCode == programCode);
                        if (program == null)
                        {
                            ShowErrorNotification($"Program {programCode} not found for student {idnum} at row {row}. Skipping this row.");
                            continue;
                        }

                        // Handle names: If the name contains a comma assume "LastName, FirstName"; else split by space.
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

                        // Create and add the student record
                        var student = new Student
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            SchoolId = idnum,
                            Email = email,
                            ProgramId = program.Id,
                            Year = yearLevel,
                            Status = scholarship,
                            Semester = semester
                        };

                        StudentBulkCollection.Add(student);
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

            try
            {
                using var context = new ApplicationDbContext();
                using var transaction = await context.Database.BeginTransactionAsync();

                foreach (var student in StudentBulkCollection)
                {
                    if (string.IsNullOrEmpty(student.SchoolId) || string.IsNullOrEmpty(student.Email))
                    {
                        ShowErrorNotification($"Missing fields for student {student.SchoolId}");
                        return;
                    }

                    var program = context.AcademicPrograms.AsNoTracking().FirstOrDefault(p => p.Id == student.ProgramId);
                    if (program == null)
                    {
                        ShowErrorNotification($"Invalid program for student {student.SchoolId}");
                        return;
                    }

                    var existingStudent = await context.Students.FirstOrDefaultAsync(s => s.SchoolId == student.SchoolId);
                    if (existingStudent == null)
                    {
                        context.Students.Add(student);
                    }
                    else
                    {
                        context.Entry(existingStudent).CurrentValues.SetValues(student);
                    }

                }

                await context.SaveChangesAsync();

                foreach (var student in StudentBulkCollection)
                {
                    if (student.Status.Equals("Scholar", StringComparison.OrdinalIgnoreCase))
                    {
                        var subjectsToEnroll = await context.Subjects
                            .Where(s => s.ProgramId == student.ProgramId && s.Year == student.Year && s.Semester == student.Semester)
                            .ToListAsync();

                        if (subjectsToEnroll.Count == 0)
                        {
                            ShowErrorNotification($"No subjects found for student {student.SchoolId}");
                            return;
                        }

                        foreach (var subject in subjectsToEnroll)
                        {
                            var newGrade = new Grade
                            {
                                StudentId = student.Id,
                                SubjectId = subject.SubjectId,
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
                ShowErrorNotification($"Error saving students: {ex.InnerException.Message}");
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
