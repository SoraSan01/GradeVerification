using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

            Subjects = new ObservableCollection<Subject>(_context.Subjects
                            .Include(s => s.AcademicProgram)  // Ensure Program is loaded
                            .ToList());

            AddSubjectCommand = new RelayCommand(AddSubject);
            EditSubjectCommand = new RelayCommand(EditSubject, CanModifySubject);
            DeleteSubjectCommand = new RelayCommand(DeleteSubject, CanModifySubject);
        }

        public ObservableCollection<Subject> Subjects
        {
            get => _subjects;
            set { _subjects = value; OnPropertyChanged(); }
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

        private void AddSubject(object parameter)
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var addSubjectWindow = new AddSubject(context);
                    addSubjectWindow.Show();
                    RefreshSubjects();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading programs: " + ex.Message);
            }
        }

        private void EditSubject(object parameter)
        {
            if (parameter is Subject subject)
            {
                var editSubjectWindow = new EditSubject();
                editSubjectWindow.ShowDialog();
                RefreshSubjects();
            }
        }

        private void DeleteSubject(object parameter)
        {
            if (parameter is Subject subject)
            {
                _context.Subjects.Remove(subject);
                _context.SaveChanges();
                RefreshSubjects();
            }
        }

        private bool CanModifySubject(object parameter) => parameter is Subject;

        private void FilterSubjects()
        {
            var filteredSubjects = _context.Subjects.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchText))
                filteredSubjects = filteredSubjects.Where(s => s.SubjectName.Contains(SearchText) || s.SubjectCode.Contains(SearchText));

            if (!string.IsNullOrWhiteSpace(SelectedYear))
                filteredSubjects = filteredSubjects.Where(s => s.Year == SelectedYear);

            if (!string.IsNullOrWhiteSpace(SelectedSemester))
                filteredSubjects = filteredSubjects.Where(s => s.Semester == SelectedSemester);

            Subjects = new ObservableCollection<Subject>(_context.Subjects
                .Include(s => s.AcademicProgram)  // Ensure Program is loaded
                .ToList());
        }

        private void RefreshSubjects()
        {
            Subjects = new ObservableCollection<Subject>(_context.Subjects
                .Include(s => s.AcademicProgram)  // Ensure Program is loaded
                .ToList());
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
