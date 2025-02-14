using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Service;
using GradeVerification.View.Admin.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class EditStudentViewModel : INotifyPropertyChanged
    {
        private string _firstName;
        private string _lastName;
        private string _studentId;
        private string _email;
        private string _semester;
        private string _year;
        private string _programId;
        private string _selectedStatus;

        private readonly EditStudent _editWindow;
        private readonly Action _onUpdate;
        private readonly AcademicProgramService _programService;

        public event PropertyChangedEventHandler PropertyChanged;

        public EditStudentViewModel(Student student, EditStudent editWindow, Action onUpdate)
        {
            _editWindow = editWindow ?? throw new ArgumentNullException(nameof(editWindow));
            _onUpdate = onUpdate ?? throw new ArgumentNullException(nameof(onUpdate));
            _programService = new AcademicProgramService();

            // Initialize properties
            FirstName = student.FirstName;
            LastName = student.LastName;
            StudentId = student.StudentId;
            Email = student.Email;
            Semester = student.Semester;
            Year = student.Year;
            ProgramId = student.ProgramId;
            SelectedStatus = student.Status;

            // Dropdown options
            Statuses = new ObservableCollection<string> { "Scholar", "Non-Scholar" };
            Semesters = new ObservableCollection<string> { "First Semester", "Second Semester" };
            Years = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
            ProgramList = new ObservableCollection<AcademicProgram>();

            LoadData();

            // Commands
            UpdateStudentCommand = new RelayCommand(UpdateStudent);
            BackCommand = new RelayCommand(Back);
        }

        // Properties with INotifyPropertyChanged
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

        public string StudentId
        {
            get => _studentId;
            set { _studentId = value; OnPropertyChanged(); }
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

        public string SelectedStatus
        {
            get => _selectedStatus;
            set { _selectedStatus = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Semesters { get; }
        public ObservableCollection<string> Years { get; }
        public ObservableCollection<AcademicProgram> ProgramList { get; }
        public ObservableCollection<string> Statuses { get; }

        public ICommand UpdateStudentCommand { get; }
        public ICommand BackCommand { get; }

        private void UpdateStudent(object parameter)
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var student = context.Students.FirstOrDefault(s => s.StudentId == StudentId);

                    if (student == null)
                    {
                        MessageBox.Show("Student not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Update student details
                    student.FirstName = FirstName;
                    student.LastName = LastName;
                    student.Email = Email;
                    student.Semester = Semester;
                    student.Year = Year;
                    student.ProgramId = ProgramId;
                    student.Status = SelectedStatus;

                    context.SaveChanges();
                    MessageBox.Show("Student information updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                _onUpdate?.Invoke();
                _editWindow.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while updating student: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                var programs = _programService.GetPrograms() ?? new List<AcademicProgram>();

                ProgramList.Clear();
                foreach (var program in programs)
                {
                    ProgramList.Add(program);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load programs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back(object parameter)
        {
            _editWindow.Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
