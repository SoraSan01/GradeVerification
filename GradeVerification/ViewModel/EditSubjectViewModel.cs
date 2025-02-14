using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Service;
using GradeVerification.View.Admin.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class EditSubjectViewModel : INotifyPropertyChanged
    {
        private readonly AcademicProgramService _programService;
        private readonly Action _onUpdate; // Callback for UI refresh
        private readonly EditSubject _editWindow; // Reference to the window

        public ObservableCollection<string> YearList { get; set; }
        public ObservableCollection<AcademicProgram> ProgramList { get; set; }
        public ObservableCollection<string> SemesterList { get; set; }

        private string _subjectId;
        public string SubjectId
        {
            get => _subjectId;
            set { _subjectId = value; OnPropertyChanged(nameof(SubjectId)); }
        }

        private string _subjectCode;
        public string SubjectCode
        {
            get => _subjectCode;
            set { _subjectCode = value; OnPropertyChanged(nameof(SubjectCode)); }
        }

        private string _subjectName;
        public string SubjectName
        {
            get => _subjectName;
            set { _subjectName = value; OnPropertyChanged(nameof(SubjectName)); }
        }

        private int _units;
        public int Units
        {
            get => _units;
            set { _units = value; OnPropertyChanged(nameof(Units)); }
        }

        private string _selectedYear;
        public string SelectedYear
        {
            get => _selectedYear;
            set { _selectedYear = value; OnPropertyChanged(nameof(SelectedYear)); }
        }

        private string _selectedProgramID;
        public string SelectedProgramID
        {
            get => _selectedProgramID;
            set { _selectedProgramID = value; OnPropertyChanged(nameof(SelectedProgramID)); }
        }

        private string _selectedSemester;
        public string SelectedSemester
        {
            get => _selectedSemester;
            set { _selectedSemester = value; OnPropertyChanged(nameof(SelectedSemester)); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditSubjectViewModel(Subject subject, Action onUpdate, EditSubject editWindow)
        {
            _programService = new AcademicProgramService();
            _onUpdate = onUpdate;
            _editWindow = editWindow;

            ProgramList = new ObservableCollection<AcademicProgram>();
            YearList = new ObservableCollection<string> { "First Year", "Second Year", "Third Year", "Fourth Year" };
            SemesterList = new ObservableCollection<string> { "First Semester", "Second Semester" };

            SubjectId = subject.SubjectId;
            SubjectCode = subject.SubjectCode;
            SubjectName = subject.SubjectName;
            Units = subject.Units;
            SelectedYear = subject.Year;
            SelectedSemester = subject.Semester;
            SelectedProgramID = subject.ProgramId;

            LoadData();

            SaveCommand = new RelayCommand(async _ => await SaveSubjectAsync());
            CancelCommand = new RelayCommand(Cancel);
        }

        private async Task SaveSubjectAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var existingSubject = await context.Subjects.FindAsync(SubjectId);
                    if (existingSubject != null)
                    {
                        existingSubject.SubjectCode = SubjectCode;
                        existingSubject.SubjectName = SubjectName;
                        existingSubject.Units = Units;
                        existingSubject.Year = SelectedYear;
                        existingSubject.Semester = SelectedSemester;
                        existingSubject.ProgramId = SelectedProgramID;

                        await context.SaveChangesAsync(); // Save changes to the database
                    }
                    else
                    {
                        MessageBox.Show("Subject not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                MessageBox.Show("Subject updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                _onUpdate?.Invoke(); // Notify UI to refresh data
                _editWindow?.Close(); // Close the edit window
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            var programs = _programService.GetPrograms();
            ProgramList.Clear();

            foreach (var program in programs)
            {
                ProgramList.Add(program);
            }
        }

        private void Cancel(object parameter)
        {
            _editWindow?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
