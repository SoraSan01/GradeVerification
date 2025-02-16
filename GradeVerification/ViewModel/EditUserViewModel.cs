using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Windows;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Core;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class EditUserViewModel : INotifyPropertyChanged
    {
        private Notifier _notifier;
        private readonly Action _onUpdate;

        private User _user;
        private readonly ApplicationDbContext _dbContext;

        public User User
        {
            get => _user;
            set { _user = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Roles { get; set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditUserViewModel(User user, ApplicationDbContext dbContext, Action onUpdate)
        {
            _onUpdate = onUpdate;

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

            _user = user;
            _dbContext = dbContext;
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);

            Roles = new ObservableCollection<string> { "Admin", "Encoder", "Staff" };
        }

        private void Save(object parameter)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(User.FirstName))
            {
                ShowErrorNotification("First Name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(User.LastName))
            {
                ShowErrorNotification("Last Name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(User.Username))
            {
                ShowErrorNotification("Username is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(User.Email) || !User.Email.Contains("@"))
            {
                ShowErrorNotification("Please provide a valid email address.");
                return;
            }

            if (string.IsNullOrWhiteSpace(User.Password) || User.Password.Length < 6)
            {
                ShowErrorNotification("Password must be at least 6 characters long.");
                return;
            }

            // Save changes to the database
            try
            {
                _dbContext.SaveChanges();
                ShowSuccessNotification("User successfully saved.");
                _onUpdate?.Invoke();
                // Close the window
                Application.Current.Windows.OfType<EditUser>().FirstOrDefault()?.Close();
            }
            catch (Exception ex)
            {
                ShowErrorNotification($"Error saving user: {ex.Message}");
            }
        }

        private void Cancel(object parameter)
        {
            // Close the window without saving
            Application.Current.Windows.OfType<EditUser>().FirstOrDefault()?.Close();
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
