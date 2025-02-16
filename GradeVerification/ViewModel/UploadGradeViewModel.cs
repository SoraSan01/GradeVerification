using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Service;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Controls;
using Xceed.Words.NET;

namespace GradeVerification.ViewModel
{
    // UploadGradesViewModel.cs
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

        public UploadGradesViewModel()
        {
            _context = new ApplicationDbContext();
            BrowseCommand = new RelayCommand(BrowseFile, CanExecuteCommand);
            SaveCommand = new RelayCommand(SaveGrades, (p) => Grades.Any() && !IsProcessing);
            CancelCommand = new RelayCommand(CancelOperation, CanExecuteCommand);
        }

        private bool CanExecuteCommand(object parameter)
        {
            return !IsProcessing;
        }

        private void BrowseFile(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Word Documents|*.doc;*.docx",
                Title = "Select grade document"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                IsProcessing = true;
                FilePath = openFileDialog.FileName;

                try
                {
                    var content = GradeDocumentParser.ParseDocumentContent(openFileDialog.FileName);
                    ProcessGradesPreview(content);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Upload Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    IsProcessing = false;
                }
            }
        }

        private async void ProcessGradesPreview(List<(string StudentName, string FinalGrade)> parsedGrades)
        {
            try
            {
                _pendingGrades.Clear();
                Grades.Clear();

                var students = await _context.Students
                    .ToDictionaryAsync(s => s.FullName);

                foreach (var (studentName, finalGrade) in parsedGrades)
                {
                    if (!students.TryGetValue(studentName, out var student))
                    {
                        MessageBox.Show($"Student {studentName} not found");
                        continue;
                    }

                    _pendingGrades.Add(new Grade
                    {
                        StudentId = student.Id,
                        SubjectId = "IS-101", // Hardcode or extract from document
                        Score = finalGrade
                    });
                }

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
                var newGrades = _pendingGrades.Where(g => string.IsNullOrEmpty(g.GradeId)).ToList();

                await _context.Grade.AddRangeAsync(newGrades);
                await _context.SaveChangesAsync();

                MessageBox.Show($"Successfully saved {newGrades.Count} grades");
                CancelOperation(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving grades: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void CancelOperation(object parameter)
        {
            Grades.Clear();
            FilePath = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
