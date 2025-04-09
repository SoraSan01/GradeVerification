using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using OfficeOpenXml;
using System.Windows;
using Microsoft.Win32;
using GradeVerification.Commands;
using GradeVerification.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GradeVerification.Service;
using GradeVerification.View.Admin.Windows;
using System.Text.RegularExpressions;
using System.Globalization;
using GradeVerification.Data;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;

namespace GradeVerification.ViewModel
{
    public class UploadSubjectViewModel : INotifyPropertyChanged
    {
        private Notifier _notifier;
        private readonly Action _onUpdate;

        private string _filePath;
        private readonly SubjectService _subjectService;
        private readonly AcademicProgramService _academicProgramService;
        private readonly ApplicationDbContext _dbContext;
        private bool _canSave;

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged();
            }
        }

        public bool CanSave
        {
            get => _canSave;
            set
            {
                _canSave = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Subject> Subjects { get; set; }

        public ICommand BrowseCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public UploadSubjectViewModel(Action onUpdate)
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

            Subjects = new ObservableCollection<Subject>();
            _subjectService = new SubjectService(_dbContext);
            _academicProgramService = new AcademicProgramService();

            BrowseCommand = new RelayCommand(BrowseFile);
            SaveCommand = new RelayCommand(async _ => await SaveSubjectsAsync(), _ => CanSave);
            CancelCommand = new RelayCommand(CloseWindow);
            _onUpdate = onUpdate;
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
                LoadExcelDataAsync();
            }
        }

        private async Task LoadExcelDataAsync()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                ShowErrorNotification("Please Select a File");
                return;
            }

            try
            {
                using (var package = new ExcelPackage(new FileInfo(FilePath)))
                {
                    Subjects.Clear();

                    foreach (var worksheet in package.Workbook.Worksheets)
                    {
                        string programName = worksheet.Name;
                        var programId = await _academicProgramService.GetProgramIdByNameAsync(programName);

                        if (programId == null)
                        {
                            MessageBox.Show($"Program '{programName}' not found in the database.");
                            continue;
                        }

                        int rowCount = worksheet.Dimension.Rows;
                        string currentYear = "";

                        for (int row = 1; row <= rowCount; row++)
                        {
                            var cellA = worksheet.Cells[row, 1].Text?.Trim();

                            // Detect Year (e.g., "First Year")
                            if (IsYearHeader(cellA))
                            {
                                currentYear = FormatYear(cellA); // Format the year to title case
                                continue;
                            }

                            // Check for semester headers (First Semester and Second Semester in the same row)
                            var cellE = worksheet.Cells[row, 5].Text?.Trim();
                            if (cellA?.Equals("First Semester", StringComparison.OrdinalIgnoreCase) == true &&
                                cellE?.Equals("Second Semester", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                // Skip the column headers row (next row)
                                row++;
                                if (row > rowCount) break;

                                // Process course rows
                                for (row++; row <= rowCount; row++)
                                {
                                    // Check if new section starts
                                    var nextCellA = worksheet.Cells[row, 1].Text?.Trim();
                                    if (IsYearHeader(nextCellA) || IsSemesterHeader(nextCellA, worksheet.Cells[row, 5].Text?.Trim()))
                                    {
                                        row--; // Adjust to reprocess this row in outer loop
                                        break;
                                    }

                                    // Process First Semester (Columns A, B, D)
                                    var code1 = worksheet.Cells[row, 1].Text?.Trim();
                                    var title1 = worksheet.Cells[row, 2].Text?.Trim();
                                    var units1Text = worksheet.Cells[row, 4].Text?.Trim();

                                    if (!string.IsNullOrEmpty(code1) && !string.IsNullOrEmpty(title1))
                                    {
                                        if (TryParseUnits(units1Text, out int units1))
                                        {
                                            Application.Current.Dispatcher.Invoke(() => Subjects.Add(new Subject
                                            {
                                                SubjectCode = code1,
                                                SubjectName = title1,
                                                Units = units1,
                                                Year = currentYear,
                                                Semester = "First Semester",
                                                ProgramId = programId
                                            }));
                                        }
                                    }

                                    // Process Second Semester (Columns E, F, G)
                                    var code2 = worksheet.Cells[row, 5].Text?.Trim();
                                    var title2 = worksheet.Cells[row, 6].Text?.Trim();
                                    var units2Text = worksheet.Cells[row, 7].Text?.Trim();

                                    if (!string.IsNullOrEmpty(code2) && !string.IsNullOrEmpty(title2))
                                    {
                                        if (TryParseUnits(units2Text, out int units2))
                                        {
                                            Application.Current.Dispatcher.Invoke(() => Subjects.Add(new Subject
                                            {
                                                SubjectCode = code2,
                                                SubjectName = title2,
                                                Units = units2,
                                                Year = currentYear,
                                                Semester = "Second Semester",
                                                ProgramId = programId
                                            }));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    CanSave = Subjects.Count > 0;
                    OnPropertyChanged(nameof(Subjects));
                }
            }
            catch (Exception ex)
            {
                ShowErrorNotification("Error loading Excel data");
                MessageBox.Show($"Error loading Excel data: {ex.Message}");
            }
        }

        // Helper method to format the year string to title case
        private string FormatYear(string year)
        {
            if (string.IsNullOrWhiteSpace(year)) return year;

            // Remove extra spaces and normalize
            string normalized = Regex.Replace(year.Trim(), @"\s+", " ");

            // Capitalize the first letter of each word
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalized.ToLower());
        }

        private bool IsYearHeader(string cellText)
        {
            if (string.IsNullOrWhiteSpace(cellText))
                return false;

            // Normalize spacing and check for year patterns
            string normalized = Regex.Replace(cellText.Trim(), @"\s+", " ");
            return normalized.Equals("First Year", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("Second Year", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("Third Year", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Equals("Fourth Year", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSemesterHeader(string cellA, string cellE)
        {
            return cellA?.Contains("Semester", StringComparison.OrdinalIgnoreCase) == true ||
                   cellE?.Contains("Semester", StringComparison.OrdinalIgnoreCase) == true;
        }

        private bool TryParseUnits(string unitsText, out int units)
        {
            units = 0;
            if (string.IsNullOrWhiteSpace(unitsText)) return false;

            // Remove non-numeric characters (e.g., parentheses)
            string cleaned = new string(unitsText.Where(c => char.IsDigit(c)).ToArray());
            return int.TryParse(cleaned, out units);
        }

        private async Task SaveSubjectsAsync()
        {
            if (Subjects == null || !Subjects.Any())
            {
                ShowErrorNotification("No subjects to save.");
                return;
            }

            try
            {
                // Ensure no duplicate subjects before saving
                var distinctSubjects = Subjects
                    .GroupBy(s => new { s.SubjectCode, s.Semester, s.Year, s.ProgramId })
                    .Select(g => g.First())
                    .ToList();

                // Convert List to ObservableCollection before passing it
                var observableSubjects = new ObservableCollection<Subject>(distinctSubjects);

                await _subjectService.SaveSubjectsAsync(observableSubjects);

                ShowSuccessNotification("Subjects successfully saved.");
                _onUpdate?.Invoke();

                // Refresh UI: Clear and reset `Subjects`
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Subjects = new ObservableCollection<Subject>(); // Reassign collection
                    OnPropertyChanged(nameof(Subjects)); // Notify UI to update
                });

                CanSave = false;
            }
            catch (Exception ex)
            {
                ShowErrorNotification("Error saving subjects");
                MessageBox.Show($"Error saving subjects: {ex.Message}");
            }
        }

        private void CloseWindow(object parameter)
        {
            Application.Current.Windows.OfType<UploadSubject>().FirstOrDefault()?.Close();
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
