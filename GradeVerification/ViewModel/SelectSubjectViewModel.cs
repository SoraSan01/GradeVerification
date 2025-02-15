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
    public class SelectSubjectViewModel : INotifyPropertyChanged
    {
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

        public ObservableCollection<Subject> Subjects { get; set; }

        public ICommand SelectCommand { get; }

        public SelectSubjectViewModel()
        {
            Subjects = new ObservableCollection<Subject>();
            SelectCommand = new RelayCommand(SelectSubject);

            LoadSubjects();
        }

        private void LoadSubjects()
        {
            using (var context = new ApplicationDbContext())
            {
                var subjects = context.Subjects.ToList();
                foreach (var subject in subjects)
                {
                    Subjects.Add(subject);
                }
            }
        }

        private void SelectSubject(object parameter)
        {
            if (SelectedSubject == null)
            {
                MessageBox.Show("Please select a subject.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Close the window
            Application.Current.Windows.OfType<SelectSubject>().FirstOrDefault()?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}