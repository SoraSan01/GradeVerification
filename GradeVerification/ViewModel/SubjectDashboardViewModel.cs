using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class SubjectDashboardViewModel : INotifyPropertyChanged
    {
        private string _searchText;
        private string _selectedYear;
        private string _selectedSemester;
        private ObservableCollection<Subject> _subjects;
        private readonly ApplicationDbContext _context;

        public event PropertyChangedEventHandler PropertyChanged;

        public SubjectDashboardViewModel()
        {
            _context = new ApplicationDbContext();

            YearOptions = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
            SemesterOptions = new ObservableCollection<string> { "First Semester", "Second Semester" };

            LoadSubjectAsync();

            AddSubjectCommand = new RelayCommand(AddSubject);
            EditSubjectCommand = new RelayCommand(EditSubject, CanModifySubject);
            DeleteSubjectCommand = new RelayCommand(DeleteSubject, CanModifySubject);
            BulkInsertCommand = new RelayCommand(BulkInsert);
        }

        public ObservableCollection<Subject> Subjects
        {
            get => _subjects;
            set
            {
                _subjects = value;
                OnPropertyChanged(); // Notify the UI
            }
        }

        public ObservableCollection<string> YearOptions { get; }
        public ObservableCollection<string> SemesterOptions { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterSubjects();
            }
        }

        public string SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged();
                FilterSubjects();
            }
        }

        public string SelectedSemester
        {
            get => _selectedSemester;
            set
            {
                _selectedSemester = value;
                OnPropertyChanged();
                FilterSubjects();
            }
        }

        public ICommand AddSubjectCommand { get; }
        public ICommand EditSubjectCommand { get; }
        public ICommand DeleteSubjectCommand { get; }
        public ICommand BulkInsertCommand { get; }


        private async void LoadSubjectAsync()
        {
            try
            {
                var subjectList = await _context.Subjects
                                                .ToListAsync();

                Subjects = new ObservableCollection<Subject>(subjectList);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading subjects: " + ex.Message);
            }
        }

        private void BulkInsert(object parameter)
        {
            var uploadSubject = new UploadSubject();
            uploadSubject.DataContext = new UploadSubjectViewModel();
            uploadSubject.Show();
        }
        private void AddSubject(object parameter)
        {
            try
            {
                var addSubjectWindow = new AddSubject
                {
                    DataContext = new AddSubjectViewModel(_context)
                };

                if (addSubjectWindow.ShowDialog() == true)
                {
                     LoadSubjectAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding Subject: {ex.Message}");
            }
        }

        private void EditSubject(object parameter)
        {
            if (parameter is Subject selectedSubject)
            {
                var editWindow = new EditSubject(selectedSubject);
                var viewModel = new EditSubjectViewModel(selectedSubject, LoadSubjectAsync, editWindow);
                editWindow.DataContext = viewModel;
                editWindow.ShowDialog();

            }
        }

        private void DeleteSubject(object parameter)
        {
            if (parameter is Subject subject)
            {
                _context.Subjects.Remove(subject);
                _context.SaveChanges();
                LoadSubjectAsync();
            }
        }

        private bool CanModifySubject(object parameter) => parameter is Subject;

        private void FilterSubjects()
        {
            var query = _context.Subjects
                .Include(s => s.AcademicProgram)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(s => s.SubjectName.Contains(SearchText) || s.SubjectCode.Contains(SearchText));

            if (!string.IsNullOrWhiteSpace(SelectedYear))
                query = query.Where(s => s.Year == SelectedYear);

            if (!string.IsNullOrWhiteSpace(SelectedSemester))
                query = query.Where(s => s.Semester == SelectedSemester);

            Subjects = new ObservableCollection<Subject>(query.ToList());
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
