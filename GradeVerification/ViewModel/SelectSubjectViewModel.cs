using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class SelectSubjectViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        private Notifier _notifier;
        private string _searchText;
        private string _selectedProgram;
        private string _selectedYear;
        private string _selectedSemester;
        private readonly Student _selectedStudent;

        public ObservableCollection<Subject> Subjects { get; set; }
        public ObservableCollection<string> Programs { get; set; }
        public ObservableCollection<string> Years { get; set; }
        public ObservableCollection<string> Semesters { get; set; }

        public ICommand SaveSelectedSubjectsCommand { get; }
        public ICommand CancelCommand { get; }

        private Subject _selectedSubject;
        public Subject SelectedSubject
        {
            get => _selectedSubject;
            set
            {
                _selectedSubject = value;
                OnPropertyChanged(nameof(SelectedSubject));
            }
        }

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

        public string SelectedProgram
        {
            get => _selectedProgram;
            set
            {
                _selectedProgram = value;
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

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public SelectSubjectViewModel(Student student)
        {
            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            _selectedStudent = student;
            Subjects = new ObservableCollection<Subject>();
            Programs = new ObservableCollection<string>();
            Years = new ObservableCollection<string>();
            Semesters = new ObservableCollection<string>();

            SaveSelectedSubjectsCommand = new RelayCommand(SaveSelectedSubjects);
            CancelCommand = new RelayCommand(Cancel);

            LoadFilters();
            LoadSubjects();
        }

        private void LoadFilters()
        {
            using (var context = new ApplicationDbContext())
            {
                Programs = new ObservableCollection<string>(context.Subjects.Select(s => s.AcademicProgram.ProgramCode).Distinct().ToList());
                Years = new ObservableCollection<string>(context.Subjects.Select(s => s.Year).Distinct().ToList());
                Semesters = new ObservableCollection<string>(context.Subjects.Select(s => s.Semester).Distinct().ToList());
            }
        }

        private void LoadSubjects()
        {
            using (var context = new ApplicationDbContext())
            {
                var subjects = context.Subjects.AsNoTracking().ToList();
                foreach (var subject in subjects)
                {
                    subject.IsSelected = false; // Ensure this line is present
                    Subjects.Add(subject);
                }
            }
        }

        private async void FilterSubjects()
        {
            await Task.Run(() =>
            {
                using (var context = new ApplicationDbContext())
                {
                    var filteredSubjects = context.Subjects.AsQueryable();
                    if (!string.IsNullOrWhiteSpace(SearchText))
                        filteredSubjects = filteredSubjects.Where(s => s.SubjectName.Contains(SearchText) || s.SubjectCode.Contains(SearchText));
                    if (!string.IsNullOrWhiteSpace(SelectedProgram))
                        filteredSubjects = filteredSubjects.Where(s => s.AcademicProgram.ProgramCode == SelectedProgram);
                    if (!string.IsNullOrWhiteSpace(SelectedYear))
                        filteredSubjects = filteredSubjects.Where(s => s.Year == SelectedYear);
                    if (!string.IsNullOrWhiteSpace(SelectedSemester))
                        filteredSubjects = filteredSubjects.Where(s => s.Semester == SelectedSemester);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Subjects.Clear();
                        foreach (var subject in filteredSubjects.ToList())
                            Subjects.Add(subject);
                    });
                }
            });
        }

        private void SaveSelectedSubjects(object parameter)
        {
            var selectedSubjects = Subjects.Where(s => s.IsSelected).ToList();

            if (!selectedSubjects.Any())
            {
                ShowErrorNotification("Please select at least one subject!");
                return;
            }

            using (var context = new ApplicationDbContext())
            {
                var existingSubjects = context.Grade
                    .Where(g => g.StudentId == _selectedStudent.Id)
                    .Select(g => g.SubjectId)
                    .ToList();

                foreach (var subject in selectedSubjects)
                {
                    if (!existingSubjects.Contains(subject.SubjectId))
                    {
                        var studentSubject = new Grade
                        {
                            StudentId = _selectedStudent.Id,
                            SubjectId = subject.SubjectId,
                            Score = null
                        };
                        context.Grade.Add(studentSubject);
                    }
                }
                context.SaveChanges();
            }
            ShowSuccessNotification("Subjects added successfully!");
            Application.Current.Windows.OfType<SelectSubject>().FirstOrDefault()?.Close();
        }

        private void Cancel(object parameter)
        {
            SelectedSubject = null;  // Ensure reset
            CloseWindow();
        }

        private void CloseWindow()
        {
            var selectSubjectWindow = Application.Current.Windows.OfType<SelectSubject>().FirstOrDefault();
            if (selectSubjectWindow != null)
            {
                selectSubjectWindow.Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ShowSuccessNotification(string message)
        {
            _notifier.ShowSuccess(message);
        }

        private void ShowErrorNotification(string message)
        {
            _notifier.ShowError(message);
        }
    }
}
