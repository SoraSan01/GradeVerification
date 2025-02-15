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

namespace GradeVerification.ViewModel
{
    public class UploadSubjectViewModel : INotifyPropertyChanged
    {
        private string _filePath;
        private readonly SubjectService _subjectService;
        private readonly AcademicProgramService _academicProgramService;
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

        public UploadSubjectViewModel()
        {
            Subjects = new ObservableCollection<Subject>();
            _subjectService = new SubjectService();
            _academicProgramService = new AcademicProgramService();

            BrowseCommand = new RelayCommand(BrowseFile);
            SaveCommand = new RelayCommand(async _ => await SaveSubjectsAsync(), _ => CanSave);
            CancelCommand = new RelayCommand(CloseWindow);
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
                MessageBox.Show("Please select a file first.");
                return;
            }

            try
            {
                using (var package = new ExcelPackage(new FileInfo(FilePath)))
                {
                    // Clear existing subjects to avoid duplication
                    Subjects.Clear();

                    // Iterate through each worksheet in the Excel file
                    foreach (var worksheet in package.Workbook.Worksheets)
                    {
                        // Extract the program name from the sheet name or a specific cell
                        string programName = worksheet.Name; // Assuming the sheet name is the program name
                        var programId = await _academicProgramService.GetProgramIdByNameAsync(programName);

                        if (programId == null)
                        {
                            MessageBox.Show($"Program '{programName}' not found in the database.");
                            continue;
                        }

                        // Iterate through the rows to find the year and semester
                        int rowCount = worksheet.Dimension.Rows;
                        string currentYear = "";
                        string currentSemester = "";

                        bool isFirstSemester = true; // Flag to track first semester
                        for (int row = 1; row <= rowCount; row++)
                        {
                            var cellValue = worksheet.Cells[row, 1].Text?.Trim();

                            // Identify Year Level
                            if (!string.IsNullOrWhiteSpace(cellValue) &&
                                (cellValue.Contains("First Year", StringComparison.OrdinalIgnoreCase) ||
                                 cellValue.Contains("Second Year", StringComparison.OrdinalIgnoreCase) ||
                                 cellValue.Contains("Third Year", StringComparison.OrdinalIgnoreCase) ||
                                 cellValue.Contains("Fourth Year", StringComparison.OrdinalIgnoreCase)))
                            {
                                currentYear = cellValue.Trim();
                                continue;
                            }

                            // Identify Semester
                            if (!string.IsNullOrWhiteSpace(cellValue) &&
                                cellValue.Contains("First Semester", StringComparison.OrdinalIgnoreCase))
                            {
                                currentSemester = cellValue.Trim();
                                continue;
                            }

                            // Process Data for Columns (1, 2, 4)
                            if (!string.IsNullOrWhiteSpace(currentYear) && !string.IsNullOrWhiteSpace(currentSemester))
                            {
                                var courseCode1 = worksheet.Cells[row, 1].Text?.Trim();
                                var descriptiveTitle1 = worksheet.Cells[row, 2].Text?.Trim();
                                var unitsText1 = worksheet.Cells[row, 4].Text?.Trim();

                                if (!string.IsNullOrWhiteSpace(courseCode1) &&
                                    !string.IsNullOrWhiteSpace(descriptiveTitle1) &&
                                    !courseCode1.Contains("Course Code") &&
                                    !descriptiveTitle1.Contains("Descriptive Title"))
                                {
                                    int.TryParse(unitsText1, out int units1);
                                    var subject = new Subject
                                    {
                                        SubjectCode = courseCode1,
                                        SubjectName = descriptiveTitle1,
                                        Units = units1,
                                        Year = currentYear,
                                        Semester = currentSemester,
                                        ProgramId = programId // Set the ProgramId from the database
                                    };

                                    Subjects.Add(subject);
                                }
                            }
                        }

                        // Second Loop: Process Columns (5, 6, 7)
                        for (int row = 1; row <= rowCount; row++)
                        {
                            var cellValue = worksheet.Cells[row, 1].Text?.Trim();

                            // Identify Year Level
                            if (!string.IsNullOrWhiteSpace(cellValue) &&
                                (cellValue.Contains("First Year", StringComparison.OrdinalIgnoreCase) ||
                                 cellValue.Contains("Second Year", StringComparison.OrdinalIgnoreCase) ||
                                 cellValue.Contains("Third Year", StringComparison.OrdinalIgnoreCase) ||
                                 cellValue.Contains("Fourth Year", StringComparison.OrdinalIgnoreCase)))
                            {
                                currentYear = cellValue.Trim();
                                continue;
                            }

                            // Process Data for Columns (5, 6, 7)
                            if (!string.IsNullOrWhiteSpace(currentYear) && !string.IsNullOrWhiteSpace(currentSemester))
                            {
                                var courseCode2 = worksheet.Cells[row, 5].Text?.Trim();
                                var descriptiveTitle2 = worksheet.Cells[row, 6].Text?.Trim();
                                var unitsText2 = worksheet.Cells[row, 7].Text?.Trim();

                                if (!string.IsNullOrWhiteSpace(courseCode2) &&
                                    !string.IsNullOrWhiteSpace(descriptiveTitle2) &&
                                    !courseCode2.Contains("Course Code") &&
                                    !descriptiveTitle2.Contains("Descriptive Title"))
                                {
                                    int.TryParse(unitsText2, out int units2);
                                    var subject = new Subject
                                    {
                                        SubjectCode = courseCode2,
                                        SubjectName = descriptiveTitle2,
                                        Units = units2,
                                        Year = currentYear,
                                        Semester = "Second Semester", // Always assign to Second Semester
                                        ProgramId = programId // Set the ProgramId from the database
                                    };

                                    Subjects.Add(subject);
                                }
                            }
                        }

                        // After processing each sheet, check if Save can be enabled
                        CanSave = Subjects.Count > 0;

                        // Notify UI update
                        OnPropertyChanged(nameof(Subjects));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private async Task SaveSubjectsAsync()
        {
            if (!Subjects.Any())
            {
                MessageBox.Show("No subjects to save.");
                return;
            }

            try
            {
                foreach (var subject in Subjects)
                {
                    // Log or check subject values
                    Console.WriteLine($"Saving subject: {subject.SubjectCode} - {subject.SubjectName}");
                }

                var success = await _subjectService.SaveSubjectsAsync(Subjects);
                if (success)
                {
                    MessageBox.Show("Subjects successfully saved.");
                    Subjects.Clear();  // Clear after saving
                    CanSave = false;    // Disable save button
                }
                else
                {
                    MessageBox.Show("An error occurred while saving subjects.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
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
    }
}
