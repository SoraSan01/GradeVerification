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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class StudentDashboardViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Student> Students { get; set; } = new ObservableCollection<Student>();
        private ObservableCollection<Student> _allStudents = new ObservableCollection<Student>();

        public ObservableCollection<string> Semesters { get; set; } = new ObservableCollection<string> { "First Semester", "Second Semester", "Summer" };
        public ObservableCollection<string> Years { get; set; } = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
        public ObservableCollection<string> Programs { get; set; } = new ObservableCollection<string>();


        private string _searchText;
        private string _selectedSemester;
        private string _selectedYear;
        private string _selectedProgram;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterStudents();
            }
        }

        public string SelectedSemester
        {
            get => _selectedSemester;
            set
            {
                _selectedSemester = value;
                OnPropertyChanged();
                FilterStudents();
            }
        }

        public string SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged();
                FilterStudents();
            }
        }

        public string SelectedProgram
        {
            get => _selectedProgram;
            set
            {
                _selectedProgram = value;
                OnPropertyChanged();
                FilterStudents();
            }
        }

        public ICommand AddStudentCommand { get; }
        public ICommand EditStudentCommand { get; }
        public ICommand DeleteStudentCommand { get; }

        public StudentDashboardViewModel()
        {
            AddStudentCommand = new RelayCommand(AddStudent);
            EditStudentCommand = new RelayCommand(EditStudent, CanModifyStudent);
            DeleteStudentCommand = new RelayCommand(async param => await DeleteStudent(param), CanModifyStudent);

            LoadStudentsAsync();
        }

        private async void LoadStudentsAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var studentList = await context.Students.Include(s => s.AcademicProgram).ToListAsync();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _allStudents.Clear();
                        Students.Clear();
                        Programs.Clear();

                        foreach (var student in studentList)
                        {
                            _allStudents.Add(student);
                            Students.Add(student);

                            if (!Programs.Contains(student.AcademicProgram.ProgramCode))
                            {
                                Programs.Add(student.AcademicProgram.ProgramCode);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading students: {ex.Message}");
            }
        }

        private void FilterStudents()
        {
            Students.Clear();
            foreach (var student in _allStudents)
            {
                bool matchesSearch = string.IsNullOrWhiteSpace(SearchText) ||
                                     student.FullName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     student.SchoolId.ToString().Contains(SearchText);

                bool matchesSemester = string.IsNullOrWhiteSpace(SelectedSemester) || student.Semester == SelectedSemester;
                bool matchesYear = string.IsNullOrWhiteSpace(SelectedYear) || student.Year == SelectedYear;
                bool matchesProgram = string.IsNullOrWhiteSpace(SelectedProgram) || student.AcademicProgram.ProgramCode == SelectedProgram;

                if (matchesSearch && matchesSemester && matchesYear && matchesProgram)
                {
                    Students.Add(student);
                }
            }
        }

        private void AddStudent(object parameter)
        {
            var addStudentWindow = new AddStudent
            {
                DataContext = new AddStudentViewModel()
            };

            if (addStudentWindow.ShowDialog() == true)
            {
                LoadStudentsAsync();
            }
        }

        private void EditStudent(object parameter)
        {
            if (parameter is Student studentToEdit)
            {
                var editWindow = new EditStudent(); // Declare the window first
                editWindow.DataContext = new EditStudentViewModel(studentToEdit, editWindow, LoadStudentsAsync); // Now use it
                editWindow.Show();
            }
        }

        private async Task DeleteStudent(object parameter)
        {
            if (parameter is Student studentToDelete)
            {
                try
                {
                    using (var context = new ApplicationDbContext())
                    {
                        var student = await context.Students.FindAsync(studentToDelete.Id);
                        if (student != null)
                        {
                            context.Students.Remove(student);
                            await context.SaveChangesAsync();
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() => Students.Remove(studentToDelete));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error deleting student: {ex.Message}");
                }
            }
        }

        private bool CanModifyStudent(object parameter) => parameter is Student;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
