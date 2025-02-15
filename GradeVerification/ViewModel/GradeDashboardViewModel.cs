using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class GradeDashboardViewModel : INotifyPropertyChanged
    {
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterGrades();
            }
        }

        private ObservableCollection<Grade> _grades;
        public ObservableCollection<Grade> Grades
        {
            get => _grades;
            set
            {
                _grades = value;
                OnPropertyChanged(nameof(Grades));
            }
        }

        private ObservableCollection<string> _semesters;
        public ObservableCollection<string> Semesters
        {
            get => _semesters;
            set
            {
                _semesters = value;
                OnPropertyChanged(nameof(Semesters));
            }
        }

        private string _selectedSemester;
        public string SelectedSemester
        {
            get => _selectedSemester;
            set
            {
                _selectedSemester = value;
                OnPropertyChanged(nameof(SelectedSemester));
                FilterGrades();
            }
        }

        private ObservableCollection<string> _years;
        public ObservableCollection<string> Years
        {
            get => _years;
            set
            {
                _years = value;
                OnPropertyChanged(nameof(Years));
            }
        }

        private string _selectedYear;
        public string SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged(nameof(SelectedYear));
                FilterGrades();
            }
        }

        private ObservableCollection<string> _programs;
        public ObservableCollection<string> Programs
        {
            get => _programs;
            set
            {
                _programs = value;
                OnPropertyChanged(nameof(Programs));
            }
        }

        private string _selectedProgram;
        public string SelectedProgram
        {
            get => _selectedProgram;
            set
            {
                _selectedProgram = value;
                OnPropertyChanged(nameof(SelectedProgram));
                FilterGrades();
            }
        }

        public ICommand DeleteGradeCommand { get; }
        public ICommand UploadFileCommand { get; }
        public ICommand InputGradeCommand { get; }


        public GradeDashboardViewModel()
        {
            Grades = new ObservableCollection<Grade>();
            Semesters = new ObservableCollection<string> { "1st Semester", "2nd Semester" };
            Years = new ObservableCollection<string> { "2023", "2024" };
            Programs = new ObservableCollection<string> { "Program A", "Program B" };

            DeleteGradeCommand = new RelayCommand(DeleteGrade);
            UploadFileCommand = new RelayCommand(UploadGrades);
            InputGradeCommand = new RelayCommand(InputGrade);


            LoadGrades();
        }

        private void LoadGrades()
        {
            using (var context = new ApplicationDbContext())
            {
                var grades = context.Grade
                                     .Include(g => g.Student)
                                     .Include(g => g.Subject)
                                     .ToList();
                Grades = new ObservableCollection<Grade>(grades);
            }
        }

        private void FilterGrades()
        {
            using (var context = new ApplicationDbContext())
            {
                var query = context.Grade
                                   .Include(g => g.Student)
                                   .Include(g => g.Subject)
                                   .AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(g => g.Student.FullName.Contains(SearchText) || g.Student.SchoolId.Contains(SearchText));
                }

                if (!string.IsNullOrWhiteSpace(SelectedSemester))
                {
                    query = query.Where(g => g.Subject.Semester == SelectedSemester);
                }

                if (!string.IsNullOrWhiteSpace(SelectedYear))
                {
                    query = query.Where(g => g.Subject.Year == SelectedYear);
                }

                if (!string.IsNullOrWhiteSpace(SelectedProgram))
                {
                    query = query.Where(g => g.Subject.ProgramId == SelectedProgram);
                }

                Grades = new ObservableCollection<Grade>(query.ToList());
            }
        }


        private void InputGrade(object parameter)
        {
            if (parameter is Grade selectedGrade)
            {
                try
                {
                    var inputGradeWindow = new InputGrade();
                    var inputGradeViewModel = new InputGradeViewModel(selectedGrade); // Pass selected grade
                    inputGradeWindow.DataContext = inputGradeViewModel;
                    inputGradeWindow.ShowDialog(); // Use ShowDialog to ensure modal behavior

                    // Refresh the data after editing
                    LoadGrades();
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

        private void DeleteGrade(object parameter)
        {
            if (parameter is Grade selectedGrade)
            {
                var result = MessageBox.Show("Are you sure you want to delete this grade?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new ApplicationDbContext())
                    {
                        context.Grade.Remove(selectedGrade);
                        context.SaveChanges();
                    }
                    LoadGrades(); // Refresh grades after deletion
                }
            }
        }

        private void UploadGrades(object parameter)
        {
            var uploadGrade = new UploadGrades();
            uploadGrade.DataContext = new UploadGradesViewModel(); // Pass selected grade
            uploadGrade.ShowDialog(); // Use ShowDialog to ensure modal behavior
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}