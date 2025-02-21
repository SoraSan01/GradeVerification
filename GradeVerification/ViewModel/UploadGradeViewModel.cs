using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Service;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
    public class UploadGradesViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationDbContext _context;
        private string _filePath;
        private bool _isProcessing;
        private List<Grade> _pendingGrades = new();

        public ObservableCollection<Grade> Grades { get; } = new();
        public ICommand BrowseCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SaveCommand { get; }

        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { _isProcessing = value; OnPropertyChanged(); }
        }

        private Notifier _notifier;

        public UploadGradesViewModel()
        {
            _context = new ApplicationDbContext();
            BrowseCommand = new RelayCommand(BrowseFile, CanExecuteCommand);
            SaveCommand = new RelayCommand(SaveGrades, (p) => Grades.Any() && !IsProcessing);
            CancelCommand = new RelayCommand(CancelOperation, CanExecuteCommand);

            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });
        }

        private bool CanExecuteCommand(object parameter)
        {
            return !IsProcessing;
        }

        private async void BrowseFile(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Word Documents|*.docx", // DocX only supports .docx
                Title = "Select grade document"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IsProcessing = true;
                FilePath = openFileDialog.FileName;

                try
                {
                    if (!File.Exists(FilePath))
                    {
                        _notifier.ShowError("The selected file does not exist.");
                        IsProcessing = false;
                        return;
                    }

                    // Parse grades from document
                    var content = GradeDocumentParser.ParseDocumentContent(FilePath);
                    if (content == null || !content.Any())
                    {
                        _notifier.ShowError("No grade data found in the file.");
                        IsProcessing = false;
                        return;
                    }

                    // Extract subject code from filename (e.g. "IS" from "IS-101.docx")
                    var fileName = Path.GetFileNameWithoutExtension(FilePath);
                    var subjectCode = fileName.Split('-')[0].Trim();

                    var subject = await _context.Subjects
                        .FirstOrDefaultAsync(s => s.SubjectCode.StartsWith(subjectCode));

                    if (subject == null)
                    {
                        _notifier.ShowError($"Subject '{subjectCode}' not found.");
                        IsProcessing = false;
                        return;
                    }

                    // Process grades with the retrieved subject.
                    await ProcessGradesPreview(content, subject);
                }
                catch (Exception ex)
                {
                    _notifier.ShowError($"Error: {ex.Message}");
                    IsProcessing = false;
                }
            }
        }

        private async Task ProcessGradesPreview(List<(string StudentName, string FinalGrade)> parsedGrades, Subject subject)
        {
            try
            {
                _pendingGrades.Clear();
                Grades.Clear();

                // Build a dictionary of students using GroupBy to avoid duplicate keys.
                var students = (await _context.Students.ToListAsync())
                    .GroupBy(s => FormatKey(s.FullName), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var missingStudents = new List<string>();

                foreach (var (studentName, finalGrade) in parsedGrades)
                {
                    // Validate that the grade value is not empty.
                    if (string.IsNullOrWhiteSpace(finalGrade))
                        continue;

                    var formattedName = FormatKey(studentName);
                    if (!students.TryGetValue(formattedName, out var student))
                    {
                        missingStudents.Add(studentName);
                        continue;
                    }

                    _pendingGrades.Add(new Grade
                    {
                        StudentId = student.Id,
                        Student = student, // Populate navigation property
                        SubjectId = subject.SubjectId,
                        Subject = subject, // Populate navigation property
                        Score = finalGrade
                    });
                }

                // Display warnings for missing students, if any.
                if (missingStudents.Any())
                {
                    _notifier.ShowWarning($"Missing students:\n{string.Join("\n", missingStudents)}");
                }

                // Update UI with processed grades.
                Grades.Clear();
                foreach (var grade in _pendingGrades)
                {
                    Grades.Add(grade);
                }
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async void SaveGrades(object parameter)
        {
            try
            {
                IsProcessing = true;
                int updateCount = 0;
                int missingCount = 0;

                // Loop through each pending grade to update the existing records.
                foreach (var pendingGrade in _pendingGrades)
                {
                    var existingGrade = await _context.Grade
                        .FirstOrDefaultAsync(g => g.StudentId == pendingGrade.StudentId &&
                                                  g.SubjectId == pendingGrade.SubjectId);
                    if (existingGrade != null)
                    {
                        // Update the score field.
                        existingGrade.Score = pendingGrade.Score;
                        updateCount++;
                    }
                    else
                    {
                        missingCount++;
                    }
                }

                await _context.SaveChangesAsync();
                _notifier.ShowSuccess($"Successfully updated {updateCount} grade(s).{(missingCount > 0 ? $" {missingCount} grade record(s) were not found and skipped." : "")}");
                CancelOperation(null);
            }
            catch (Exception ex)
            {
                _notifier.ShowError($"Error saving grades: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private string FormatKey(string input)
        {
            return input?.Trim().ToLower(); // Ensures consistent comparison by trimming spaces and converting to lowercase.
        }

        private void CancelOperation(object parameter)
        {
            Application.Current.Windows.OfType<UploadGrades>().FirstOrDefault()?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
