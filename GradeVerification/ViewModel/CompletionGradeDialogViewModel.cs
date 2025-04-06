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

namespace GradeVerification.ViewModel
{
    public class CompletionGradeDialogViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private readonly Grade _grade;
        private string _completionGrade;
        private DateTime _examDate;
        private Professor _selectedProfessor;
        private ExamStatus _selectedStatus;
        private readonly Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();

        public CompletionGradeDialogViewModel(Grade grade)
        {
            _grade = grade;
            SaveCompletionCommand = new RelayCommand(SaveCompletion, CanSaveCompletion);
            CancelCommand = new RelayCommand(Cancel);
            _ = LoadProfessorsAsync();

            // Initialize default values
            ExamDate = DateTime.Today;
            StatusOptions = Enum.GetValues(typeof(ExamStatus)).Cast<ExamStatus>().ToList();
        }

        #region Properties
        public Grade Grade => _grade;

        public string CompletionGrade
        {
            get => _completionGrade;
            set
            {
                _completionGrade = value;
                OnPropertyChanged();
                Validate(nameof(CompletionGrade));
            }
        }

        public DateTime ExamDate
        {
            get => _examDate;
            set
            {
                _examDate = value;
                OnPropertyChanged();
                Validate(nameof(ExamDate));
            }
        }

        public Professor SelectedProfessor
        {
            get => _selectedProfessor;
            set
            {
                _selectedProfessor = value;
                OnPropertyChanged();
                Validate(nameof(SelectedProfessor));
            }
        }

        public ExamStatus SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                _selectedStatus = value;
                OnPropertyChanged();
                Validate(nameof(SelectedStatus));
            }
        }

        public ObservableCollection<Professor> Professors { get; } = new ObservableCollection<Professor>();
        public List<ExamStatus> StatusOptions { get; }
        #endregion

        #region Commands
        public ICommand SaveCompletionCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        #region Command Handlers
        private async void SaveCompletion(object parameter)
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    // Create completion exam
                    var completionExam = new CompletionExam
                    {
                        GradeId = _grade.GradeId,
                        StudentId = _grade.StudentId,
                        ProfessorId = SelectedProfessor.Id,
                        Score = int.Parse(CompletionGrade),
                        ExamDate = ExamDate,
                        Status = SelectedStatus
                    };

                    // Update grade record
                    _grade.CompletionGrade = CompletionGrade;
                    _grade.HasCompletionExam = true;

                    context.CompletionExams.Add(completionExam);
                    context.Grade.Update(_grade);
                    await context.SaveChangesAsync();
                }

                // Close dialog
                Application.Current.Windows.OfType<CompletionGradeDialog>().FirstOrDefault()?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving completion grade: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveCompletion(object parameter) => !HasErrors;

        private void Cancel(object parameter)
        {
            Application.Current.Windows.OfType<CompletionGradeDialog>().FirstOrDefault()?.Close();
        }
        #endregion

        #region Data Loading
        private async Task LoadProfessorsAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var professors = await context.Professors.ToListAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Professors.Clear();
                        foreach (var professor in professors)
                        {
                            Professors.Add(professor);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading professors: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Validation
        private void Validate(string propertyName)
        {
            ClearErrors(propertyName);

            switch (propertyName)
            {
                case nameof(CompletionGrade):
                    if (string.IsNullOrWhiteSpace(CompletionGrade))
                        AddError(propertyName, "Completion grade is required");
                    else if (!decimal.TryParse(CompletionGrade, out decimal score) || score < 0 || score > 100)
                        AddError(propertyName, "Invalid grade (0-100)");
                    break;

                case nameof(ExamDate):
                    if (ExamDate > DateTime.Today)
                        AddError(propertyName, "Exam date cannot be in the future");
                    break;

                case nameof(SelectedProfessor):
                    if (SelectedProfessor == null)
                        AddError(propertyName, "Professor is required");
                    break;

                case nameof(SelectedStatus):
                    // Always valid as it's selected from predefined options
                    break;
            }
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region INotifyDataErrorInfo
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        public bool HasErrors => _errors.Any();

        public IEnumerable GetErrors(string propertyName)
        {
            return _errors.ContainsKey(propertyName)
                ? _errors[propertyName]
                : Enumerable.Empty<string>();
        }

        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
        #endregion
    }
}