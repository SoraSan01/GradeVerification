using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin;
using GradeVerification.View.Admin.Windows;
using GradeVerification.View.Encoder;
using GradeVerification.View.Staff;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class LoginViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private readonly Notifier _notifier;
        private readonly ApplicationDbContext _dbContext;

        private string _username;
        private string _password;
        private bool _isLoggingIn;
        private readonly Dictionary<string, List<string>> _errors = new();

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                    ValidateUsername();
                    ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                    ValidatePassword();
                    ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set
            {
                _isLoggingIn = value;
                OnPropertyChanged();
                ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand LoginCommand { get; }
        public ICommand ForgoPasstCommand { get; }

        public LoginViewModel(ApplicationDbContext dbContext)
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

            _dbContext = dbContext;

            // Command can execute only if not logging in and no validation errors exist.
            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsLoggingIn && !HasErrors);
            ForgoPasstCommand = new RelayCommand(ForgotPass);
        }

        private void ForgotPass(object parameter)
        {
            var forgotPassWindow = new ForgotPassword();
            forgotPassWindow.DataContext = new ForgotPasswordViewModel(_dbContext);
            forgotPassWindow.Show();
            Application.Current.Windows.OfType<MainWindow>().FirstOrDefault()?.Close();
        }

        private async Task LoginAsync()
        {
            if (HasErrors)
            {
                ShowErrorNotification("Please fix validation errors before logging in.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ShowErrorNotification("Please enter both username and password.");
                return;
            }

            IsLoggingIn = true;

            try
            {
                // Fetch user by username
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == Username);
                if (user != null)
                {
                    // Check hashed password (assuming passwords are stored hashed)
                    if (!VerifyPassword(Password, user.Password))
                    {
                        ShowErrorNotification("Invalid username or password.");
                        return;
                    }

                    ShowSuccessNotification("Login Successfully!");

                    // Redirect user based on role
                    switch (user.Role)
                    {
                        case "Admin":
                            new AdminWindow(_dbContext, user).Show();
                            break;
                        case "Encoder":
                            new EncoderWindow(_dbContext, user).Show();
                            break;
                        default:
                            ShowErrorNotification("Unauthorized role detected.");
                            return;
                    }

                    // Close the login window after successful login
                    Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is MainWindow)?.Close();
                }
                else
                {
                    ShowErrorNotification("Invalid username or password.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected error occurred. Please try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Log the error details if necessary.
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private bool VerifyPassword(string enteredPassword, string storedPasswordHash)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] enteredHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(enteredPassword));
            string enteredHashString = Convert.ToBase64String(enteredHash);
            return enteredHashString == storedPasswordHash;
        }

        #region Validation Methods

        public bool HasErrors => _errors.Any();

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;
            return _errors.ContainsKey(propertyName) ? _errors[propertyName] : null;
        }

        private void ValidateUsername()
        {
            ClearErrors(nameof(Username));
            if (string.IsNullOrWhiteSpace(Username))
            {
                AddError(nameof(Username), "Username is required.");
            }
            else if (Username.Length < 3)
            {
                AddError(nameof(Username), "Username must be at least 3 characters long.");
            }
        }

        private void ValidatePassword()
        {
            ClearErrors(nameof(Password));
            if (string.IsNullOrWhiteSpace(Password))
            {
                AddError(nameof(Password), "Password is required.");
            }
            else if (Password.Length < 6)
            {
                AddError(nameof(Password), "Password must be at least 6 characters long.");
            }
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
            {
                _errors[propertyName] = new List<string>();
            }
            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ShowSuccessNotification(string message) => _notifier.ShowSuccess(message);
        private void ShowErrorNotification(string message) => _notifier.ShowError(message);
    }
}
