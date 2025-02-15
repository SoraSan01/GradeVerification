﻿using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Service;
using GradeVerification.View.Admin.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class AddStudentViewModel : INotifyPropertyChanged
    {
        private readonly AcademicProgramService _programService;
        private readonly ApplicationDbContext _context;

        private string _studentId;
        private string _firstName;
        private string _lastName;
        private string _email;
        private string _semester;
        private string _year;
        private string _programId;
        private string _status;

        public string StudentId
        {
            get => _studentId;
            set { _studentId = value; OnPropertyChanged(); }
        }

        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(); }
        }

        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Semester
        {
            get => _semester;
            set { _semester = value; OnPropertyChanged(); }
        }

        public string Year
        {
            get => _year;
            set { _year = value; OnPropertyChanged(); }
        }

        public string ProgramId
        {
            get => _programId;
            set { _programId = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Statuses { get; set; }
        public ObservableCollection<string> Semesters { get; set; }
        public ObservableCollection<string> Years { get; set; }
        public ObservableCollection<AcademicProgram> ProgramList { get; }

        public ICommand SaveStudentCommand { get; }
        public ICommand BackCommand { get; }

        public AddStudentViewModel()
        {
            _programService = new AcademicProgramService();
            _context = new ApplicationDbContext();

            Statuses = new ObservableCollection<string> { "Scholar", "Non-Scholar" };
            Semesters = new ObservableCollection<string> { "First Semester", "Second Semester" };
            Years = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
            ProgramList = new ObservableCollection<AcademicProgram>();

            LoadPrograms();

            SaveStudentCommand = new RelayCommand(async param => await SaveStudent());
            BackCommand = new RelayCommand(Back);
        }

        private void LoadPrograms()
        {
            ProgramList.Clear();
            var programs = _programService.GetPrograms();
            foreach (var program in programs)
            {
                ProgramList.Add(program);
            }
        }

        private async Task SaveStudent()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(StudentId) || string.IsNullOrWhiteSpace(FirstName) ||
                    string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Email) ||
                    string.IsNullOrWhiteSpace(Semester) || string.IsNullOrWhiteSpace(Year) ||
                    string.IsNullOrWhiteSpace(ProgramId) || string.IsNullOrWhiteSpace(Status))
                {
                    MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create new student
                var newStudent = new Student
                {
                    SchoolId = StudentId, // Ensure this matches the foreign key in Grade
                    FirstName = FirstName,
                    LastName = LastName,
                    Email = Email,
                    Semester = Semester,
                    Year = Year,
                    ProgramId = ProgramId,
                    Status = Status
                };

                _context.Students.Add(newStudent);
                await _context.SaveChangesAsync();

                // Enroll the student in subjects
                var subjectsToEnroll = _context.Subjects
                    .Where(s => s.ProgramId == ProgramId && s.Year == Year && s.Semester == Semester)
                    .ToList();

                foreach (var subject in subjectsToEnroll)
                {
                    var newGrade = new Grade
                    {
                        StudentId = newStudent.Id,
                        SubjectId = subject.SubjectId,
                        Score = null // Initially null until graded
                    };

                    _context.Grade.Add(newGrade);
                }

                await _context.SaveChangesAsync();

                MessageBox.Show("Student saved and enrolled in subjects successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
                LoadPrograms(); // Refresh UI
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.InnerException?.Message ?? ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            StudentId = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            Semester = string.Empty;
            Year = string.Empty;
            ProgramId = string.Empty;
            Status = string.Empty;
        }

        private void Back(object parameter)
        {
            Application.Current.Windows.OfType<AddStudent>().FirstOrDefault()?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
