using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using Microsoft.EntityFrameworkCore;

namespace GradeVerification.ViewModel
{
    public class GradeDashboardViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Grade> _grades;
        private string _searchText;
        private string _selectedSemester;
        private string _selectedYear;
        private string _selectedProgram;

        public ObservableCollection<Grade> Grades
        {
            get => _grades;
            set { _grades = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); LoadGradesAsync(); }
        }

        public string SelectedSemester
        {
            get => _selectedSemester;
            set { _selectedSemester = value; OnPropertyChanged(); LoadGradesAsync(); }
        }

        public string SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(); LoadGradesAsync(); }
        }

        public string SelectedProgram
        {
            get => _selectedProgram;
            set { _selectedProgram = value; OnPropertyChanged(); LoadGradesAsync(); }
        }

        public ObservableCollection<string> Semesters { get; set; }
        public ObservableCollection<string> Years { get; set; }
        public ObservableCollection<string> Programs { get; set; }

        public ICommand AddGradeCommand { get; }
        public ICommand EditGradeCommand { get; }
        public ICommand DeleteGradeCommand { get; }
        public ICommand UploadFileCommand { get; }

        public GradeDashboardViewModel()
        {
            // Initialize collections
            Semesters = new ObservableCollection<string> { "First Semester", "Second Semester" };
            Years = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
            Programs = new ObservableCollection<string> { "BSCS", "BSIT", "BSECE" };

            // Initialize commands
            AddGradeCommand = new RelayCommand(AddGrade);
            EditGradeCommand = new RelayCommand(EditGrade);
            DeleteGradeCommand = new RelayCommand(DeleteGrade);
            UploadFileCommand = new RelayCommand(UploadGrades);

            // Load initial data
            LoadGradesAsync();
        }

        private async void LoadGradesAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var query = context.Grade
                        .Include(g => g.Student)
                        .Include(g => g.Subject)
                        .AsQueryable();

                    // Apply filters
                    if (!string.IsNullOrEmpty(SearchText))
                    {
                        query = query.Where(g => g.Student.FullName.Contains(SearchText));
                    }

                    if (!string.IsNullOrEmpty(SelectedSemester))
                    {
                        query = query.Where(g => g.Subject.Semester == SelectedSemester);
                    }

                    if (!string.IsNullOrEmpty(SelectedYear))
                    {
                        query = query.Where(g => g.Subject.Year == SelectedYear);
                    }

                    if (!string.IsNullOrEmpty(SelectedProgram))
                    {
                        query = query.Where(g => g.Subject.ProgramId == SelectedProgram);
                    }

                    Grades = new ObservableCollection<Grade>(await query.ToListAsync());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading grades: {ex.Message}");
            }
        }

        private void AddGrade(object parameter)
        {
            // Logic to add a new grade
            MessageBox.Show("Add Grade functionality not implemented yet.");
        }

        private void EditGrade(object parameter)
        {
            if (parameter is Grade selectedGrade)
            {
                // Logic to edit the selected grade
                MessageBox.Show($"Editing grade for student: {selectedGrade.Student.FullName}");
            }
        }

        private void DeleteGrade(object parameter)
        {
            if (parameter is Grade selectedGrade)
            {
                // Logic to delete the selected grade
                MessageBox.Show($"Deleting grade for student: {selectedGrade.Student.FullName}");
            }
        }

        private void UploadGrades(object parameter)
        {
            // Logic to upload grades from a file
            MessageBox.Show("Upload Grades functionality not implemented yet.");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}