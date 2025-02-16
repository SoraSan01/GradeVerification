using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin;
using GradeVerification.View.Admin.Windows;
using GradeVerification.View.Encoder;
using GradeVerification.View.Staff;
using Microsoft.EntityFrameworkCore;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private Notifier _notifier;

        private readonly ApplicationDbContext _dbContext;

        private string _username;
        private string _password;
        private bool _isLoggingIn;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set { _isLoggingIn = value; OnPropertyChanged(); }
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
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });


            _dbContext = dbContext;
            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsLoggingIn);
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
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ShowErrorNotification("Please enter both username and password.");
                return;
            }

            IsLoggingIn = true;

            try
            {
                // Fetch user by username
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == Username);

                if (user != null)
                {
                    // Check hashed password (assuming you're storing hashed passwords)
                    if (!VerifyPassword(Password, user.Password))
                    {
                        ShowErrorNotification("Invalid username or password.");
                        return;
                    }

                    // Welcome message
                    ShowSuccessNotification("Login Successfully!");

                    // Redirect user based on role
                    switch (user.Role)
                    {
                        case "Admin":
                            // Pass the User object to the AdminWindow constructor
                            new AdminWindow(_dbContext, user).Show();
                            break;
                        case "Staff":
                            new StaffWindow(_dbContext, user).Show();
                            break;
                        case "Encoder":
                            new EncoderWindow(_dbContext, user).Show();
                            break;
                        default:
                            ShowErrorNotification("Unauthorized role detected.");
                            return;
                    }

                    // Close login window after successful login
                    Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is MainWindow)?.Close();
                }
                else
                {
                    ShowErrorNotification("Invalid username or password.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred. Please try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MessageBox.Show($"Login Error: {ex.Message}"); // Log the error
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private bool VerifyPassword(string enteredPassword, string storedPasswordHash)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] enteredHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(enteredPassword));
                string enteredHashString = Convert.ToBase64String(enteredHash);
                return enteredHashString == storedPasswordHash;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
