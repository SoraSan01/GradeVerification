using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;

namespace GradeVerification.ViewModel
{
    public class InputGradeViewModel : INotifyPropertyChanged
    {
        private readonly Action _onUpdate;
        private Notifier _notifier;

        private string _studentName;
        private string _courseCode;
        private string _score;

        public string GradeId { get; set; }

        public string StudentName
        {
            get => _studentName;
            set { _studentName = value; OnPropertyChanged(nameof(StudentName)); }
        }

        public string CourseCode
        {
            get => _courseCode;
            set { _courseCode = value; OnPropertyChanged(nameof(CourseCode)); }
        }

        public string Score
        {
            get => _score;
            set
            {
                _score = value;
                OnPropertyChanged(nameof(Score));

                if (CurrentGrade != null)
                {
                    CurrentGrade.Score = value; // Update CurrentGrade.Score
                    OnPropertyChanged(nameof(CurrentGrade));
                }
            }
        }

        private Grade _currentGrade;
        public Grade CurrentGrade
        {
            get => _currentGrade;
            set
            {
                _currentGrade = value;
                OnPropertyChanged(nameof(CurrentGrade));
            }
        }

        public ICommand CancelCommand { get; }

        public ICommand SaveCommand { get; }

        public InputGradeViewModel(Grade grade)
        {

            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(1.5),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(3));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            CurrentGrade = grade ?? new Grade(); // If no grade is provided, create a new one
            SaveCommand = new RelayCommand(SaveGrade);
            CancelCommand = new RelayCommand(Cancel);

            StudentName = grade.Student.FullName;
            CourseCode = grade.Subject.SubjectCode;
            Score = grade.Score;
        }

        private void SaveGrade(object parameter)
        {
            if (string.IsNullOrEmpty(CurrentGrade.Student.FullName) ||
                string.IsNullOrEmpty(CurrentGrade.Subject.SubjectCode) ||
                string.IsNullOrEmpty(CurrentGrade.Score))
            {
                ShowErrorNotification("All fields are required.");
                return;
            }

            using (var context = new ApplicationDbContext())
            {
                if (string.IsNullOrEmpty(CurrentGrade.GradeId)) // New Grade
                {
                    context.Grade.Add(CurrentGrade);
                }
                else // Update existing Grade
                {
                    context.Grade.Update(CurrentGrade);
                }

                context.SaveChanges();
            }

            ShowSuccessNotification("Grade saved successfully!");
            _onUpdate?.Invoke(); // Notify main view to refresh UI
        }

        private void Cancel(object parameter)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
                window?.Close();
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
