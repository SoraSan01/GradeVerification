using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class SelectStudentViewModel : INotifyPropertyChanged
    {
        private Student _selectedStudent;
        public Student SelectedStudent
        {
            get => _selectedStudent;
            set
            {
                _selectedStudent = value;
                OnPropertyChanged(nameof(SelectedStudent));
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterStudents();
            }
        }

        public ObservableCollection<Student> Students { get; set; }
        public ObservableCollection<Student> FilteredStudents { get; set; }

        public ICommand SelectCommand { get; }
        public ICommand CancelCommand { get; }

        public SelectStudentViewModel()
        {
            Students = new ObservableCollection<Student>();
            FilteredStudents = new ObservableCollection<Student>();
            SelectCommand = new RelayCommand(SelectStudent);
            CancelCommand = new RelayCommand(Cancel);

            LoadStudents();
        }

        private void LoadStudents()
        {
            using (var context = new ApplicationDbContext())
            {
                var students = context.Students
                                      .Where(s => s.Status == "Non-Scholar") // Only non-scholar students
                                      .ToList();
                foreach (var student in students)
                {
                    Students.Add(student);
                    FilteredStudents.Add(student);
                }
            }
        }

        private void FilterStudents()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredStudents.Clear();
                foreach (var student in Students)
                {
                    FilteredStudents.Add(student);
                }
            }
            else
            {
                var filtered = Students
                    .Where(s => s.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                s.SchoolId.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                FilteredStudents.Clear();
                foreach (var student in filtered)
                {
                    FilteredStudents.Add(student);
                }
            }
        }

        private void SelectStudent(object parameter)
        {
            if (SelectedStudent == null)
            {
                MessageBox.Show("Please select a student.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Open the SelectSubject window and pass the selected student
            var selectSubjectWindow = new SelectSubject();
            selectSubjectWindow.DataContext = new SelectSubjectViewModel(SelectedStudent);
            selectSubjectWindow.ShowDialog();

            // Close the current window after subject selection
            Application.Current.Windows.OfType<SelectStudent>().FirstOrDefault()?.Close();
        }


        private void Cancel(object parameter)
        {
            // Close the window without selecting a student
            Application.Current.Windows.OfType<SelectStudent>().FirstOrDefault()?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}