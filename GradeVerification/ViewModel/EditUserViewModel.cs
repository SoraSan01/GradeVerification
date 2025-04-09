using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
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
    public class EditUserViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Notifier _notifier;
        private readonly Action _onUpdate;
        private readonly ApplicationDbContext _dbContext;

        // Backing fields for editable properties.
        private string _firstName;
        private string _lastName;
        private string _username;
        private string _email;
        private string _password;
        private string _role;

        // The ID of the user being edited.
        public string Id { get; set; }

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
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }
        // Note: In a real application, you might not want to show or edit the password in plain text.
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }
        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Roles { get; set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditUserViewModel(User user, ApplicationDbContext dbContext, Action onUpdate)
        {
            _onUpdate = onUpdate ?? throw new ArgumentNullException(nameof(onUpdate));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // Initialize notifier.
            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(1.5),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(100));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            // Initialize view model properties from the existing user.
            Id = user.Id;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Username = user.Username;
            Email = user.Email;
            Password = user.Password;
            Role = user.Role;

            Roles = new ObservableCollection<string> { "Admin", "Encoder", "Staff" };

            // Create commands. SaveCommand uses a CanExecute predicate that checks for validation errors.
            SaveCommand = new RelayCommand(Save, _ => !HasErrors);
            CancelCommand = new RelayCommand(Cancel);
        }

        #region IDataErrorInfo Implementation

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                string result = null;
                switch (columnName)
                {
                    case nameof(FirstName):
                        if (string.IsNullOrWhiteSpace(FirstName))
                            result = "First Name is required.";
                        break;
                    case nameof(LastName):
                        if (string.IsNullOrWhiteSpace(LastName))
                            result = "Last Name is required.";
                        break;
                    case nameof(Username):
                        if (string.IsNullOrWhiteSpace(Username))
                            result = "Username is required.";
                        break;
                    case nameof(Email):
                        if (string.IsNullOrWhiteSpace(Email))
                            result = "Email is required.";
                        else if (!Email.Contains("@"))
                            result = "Please provide a valid email address.";
                        break;
                    case nameof(Password):
                        if (string.IsNullOrWhiteSpace(Password))
                            result = "Password is required.";
                        else if (Password.Length < 6)
                            result = "Password must be at least 6 characters long.";
                        break;
                    case nameof(Role):
                        if (string.IsNullOrWhiteSpace(Role))
                            result = "Role must be selected.";
                        break;
                }
                return result;
            }
        }

        // Helper property that returns true if any property has a validation error.
        public bool HasErrors =>
            this[nameof(FirstName)] != null ||
            this[nameof(LastName)] != null ||
            this[nameof(Username)] != null ||
            this[nameof(Email)] != null ||
            this[nameof(Password)] != null ||
            this[nameof(Role)] != null;

        #endregion

        private void Save(object parameter)
        {
            if (HasErrors)
            {
                ShowErrorNotification("Please correct the errors before saving.");
                return;
            }

            try
            {
                // Retrieve the user from the database and update properties.
                var userToUpdate = _dbContext.Users.Find(Id);
                if (userToUpdate == null)
                {
                    ShowErrorNotification("User not found.");
                    return;
                }
                userToUpdate.FirstName = FirstName;
                userToUpdate.LastName = LastName;
                userToUpdate.Username = Username;
                userToUpdate.Email = Email;
                userToUpdate.Password = Password;
                userToUpdate.Role = Role;

                _dbContext.SaveChanges();
                ShowSuccessNotification("User successfully saved.");
                _onUpdate?.Invoke();
                Application.Current.Windows.OfType<EditUser>().FirstOrDefault()?.Close();
            }
            catch (Exception ex)
            {
                ShowErrorNotification($"Error saving user: {ex.Message}");
            }
        }

        private void Cancel(object parameter)
        {
            Application.Current.Windows.OfType<EditUser>().FirstOrDefault()?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            // Refresh the Save command's state.
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
