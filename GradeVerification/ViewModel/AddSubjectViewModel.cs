using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Service;
using GradeVerification.View.Admin.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class AddSubjectViewModel : INotifyPropertyChanged
    {
        private readonly AcademicProgramService _programService;
        private readonly ApplicationDbContext _context;

        private string _subjectCode;
        private string _subjectName;
        private int _units;
        private string _selectedYear;
        private string _selectedProgramID;
        private string _selectedSemester;

        public event PropertyChangedEventHandler PropertyChanged;

        public AddSubjectViewModel()
        {
            YearList = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
            SemesterList = new ObservableCollection<string> { "First Semester", "Second Semester" };
            
            ProgramList = new ObservableCollection<AcademicProgram>();

            _programService = new AcademicProgramService();
            _context = new ApplicationDbContext();

            LoadPrograms();

            SaveSubjectCommand = new RelayCommand(async (param) => await SaveSubject(), (param) => CanSaveSubject());
            CancelCommand = new RelayCommand(Cancel);
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

        public string SubjectCode
        {
            get => _subjectCode;
            set { _subjectCode = value; OnPropertyChanged(); }
        }

        public string SubjectName
        {
            get => _subjectName;
            set { _subjectName = value; OnPropertyChanged(); }
        }

        public int Units
        {
            get => _units;
            set { _units = value; OnPropertyChanged(); }
        }

        public string SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(); }
        }

        public string SelectedProgramID
        {
            get => _selectedProgramID;
            set { _selectedProgramID = value; OnPropertyChanged(); }
        }

        public string SelectedSemester
        {
            get => _selectedSemester;
            set { _selectedSemester = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> YearList { get; }
        public ObservableCollection<AcademicProgram> ProgramList { get; }
        public ObservableCollection<string> SemesterList { get; }

        public ICommand SaveSubjectCommand { get; }
        public ICommand CancelCommand { get; }

        private async Task SaveSubject()
        {
            try
            {
                var newSubject = new Subject
                {
                    SubjectCode = SubjectCode,
                    SubjectName = SubjectName,
                    Units = Units,
                    Year = SelectedYear,
                    ProgramId = SelectedProgramID,
                    Semester = SelectedSemester
                };

                _context.Subjects.Add(newSubject);
                await _context.SaveChangesAsync();

                MessageBox.Show("Subject saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
                LoadPrograms(); // Refresh UI
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveSubject()
        {
            return !string.IsNullOrWhiteSpace(SubjectCode) &&
                   !string.IsNullOrWhiteSpace(SubjectName) &&
                   Units > 0 &&
                   !string.IsNullOrWhiteSpace(SelectedYear) &&
                   !string.IsNullOrWhiteSpace(SelectedProgramID) &&
                   !string.IsNullOrWhiteSpace(SelectedSemester);
        }

        private void ClearForm()
        {
            SubjectCode = string.Empty;
            SubjectName = string.Empty;
            Units = 0;
            SelectedYear = null;
            SelectedProgramID = null;
            SelectedSemester = null;
        }

        private void Cancel(object parameter)
        {
            Application.Current.Windows.OfType<AddSubject>().FirstOrDefault()?.Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
