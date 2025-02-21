using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.Service;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;

namespace GradeVerification.ViewModel
{
    public class AddUserViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Notifier _notifier;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserService _userService;
        private readonly Action _onUpdate;

        private string _firstName;
        private string _lastName;
        private string _username;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _selectedRole;

        public event PropertyChangedEventHandler PropertyChanged;

        public AddUserViewModel(ApplicationDbContext dbContext, Action onUpdate)
        {
            _dbContext = dbContext;
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

            _userService = new UserService(dbContext);
            Roles = new ObservableCollection<string> { "Admin", "Staff", "Encoder" };

            // Set up the command with a CanExecute predicate that checks for validation errors.
            SaveUserCommand = new RelayCommand(async _ => await SaveUserAsync(), _ => !HasErrors());
            BackCommand = new RelayCommand(Back);
        }

        // IDataErrorInfo implementation
        public string Error => null;
        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(FirstName):
                        if (string.IsNullOrWhiteSpace(FirstName))
                            return "First Name is required.";
                        if (!FirstName.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
                            return "First Name can only contain letters and spaces.";
                        break;
                    case nameof(LastName):
                        if (string.IsNullOrWhiteSpace(LastName))
                            return "Last Name is required.";
                        if (!LastName.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
                            return "Last Name can only contain letters and spaces.";
                        break;
                    case nameof(Username):
                        if (string.IsNullOrWhiteSpace(Username))
                            return "Username is required.";
                        if (!Username.All(c => char.IsLetterOrDigit(c)))
                            return "Username can only contain letters and numbers.";
                        break;
                    case nameof(Email):
                        if (string.IsNullOrWhiteSpace(Email))
                            return "Email is required.";
                        if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                            return "Email format is invalid.";
                        break;
                    case nameof(Password):
                        if (string.IsNullOrWhiteSpace(Password))
                            return "Password is required.";
                        if (Password.Length < 8)
                            return "Password must be at least 8 characters long.";
                        break;
                    case nameof(ConfirmPassword):
                        if (string.IsNullOrWhiteSpace(ConfirmPassword))
                            return "Confirm Password is required.";
                        if (Password != ConfirmPassword)
                            return "Passwords do not match.";
                        break;
                    case nameof(SelectedRole):
                        if (string.IsNullOrWhiteSpace(SelectedRole))
                            return "Role selection is required.";
                        break;
                }
                return null;
            }
        }

        // Helper to check if any property has a validation error.
        private bool HasErrors()
        {
            string[] validatedProperties = { nameof(FirstName), nameof(LastName), nameof(Username),
                                               nameof(Email), nameof(Password), nameof(ConfirmPassword), nameof(SelectedRole) };

            return validatedProperties.Any(prop => !string.IsNullOrEmpty(this[prop]));
        }

        // Properties with OnPropertyChanged that also raise command state updates.
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
        // Note: For security reasons, PasswordBox cannot directly bind. 
        // Use the PasswordChanged events to update these properties.
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(); }
        }
        public string SelectedRole
        {
            get => _selectedRole;
            set { _selectedRole = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Roles { get; set; }

        public ICommand SaveUserCommand { get; }
        public ICommand BackCommand { get; }

        private async Task SaveUserAsync()
        {
            // Final check for validation errors.
            if (HasErrors())
            {
                ShowErrorNotification("Please fix the validation errors before saving.");
                return;
            }

            bool userAdded = await Task.Run(async () =>
            {
                try
                {
                    // Check for existing username
                    bool userExists = await _dbContext.Users.AnyAsync(u => u.Username == Username);
                    if (userExists)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ShowErrorNotification("Username already exists!");
                        });
                        return false;
                    }

                    // Generate unique User ID
                    string newUserId = await _userService.GenerateUserIdAsync();

                    // Hash the password before storing
                    string hashedPassword = HashPassword(Password);

                    var newUser = new User
                    {
                        Id = newUserId,
                        FirstName = FirstName,
                        LastName = LastName,
                        Username = Username,
                        Email = Email,
                        Password = hashedPassword,
                        Role = SelectedRole
                    };

                    _dbContext.Users.Add(newUser);
                    await _dbContext.SaveChangesAsync();

                    return true;
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return false;
                }
            });

            if (userAdded)
            {
                ShowSuccessNotification("User successfully added!");
                ClearFields();
                _onUpdate?.Invoke();
            }
        }

        private void Back(object obj)
        {
            Application.Current.Windows.OfType<AddUser>().FirstOrDefault()?.Close();
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private void ClearFields()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Username = string.Empty;
            Email = string.Empty;
            SelectedRole = null;
            Password = string.Empty;
            ConfirmPassword = string.Empty;

            // Notify the View to clear the PasswordBoxes if necessary.
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is AddUser) is AddUser window)
                {
                    window.ClearPasswordFields();
                }
            });
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            // Update the Save command's state if the validated property changes.
            (SaveUserCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
