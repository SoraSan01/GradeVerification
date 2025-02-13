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
using Microsoft.EntityFrameworkCore;

namespace GradeVerification.ViewModel
{
    public class LoginViewModel : INotifyPropertyChanged
    {
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

        public LoginViewModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsLoggingIn);
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Please enter both username and password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Welcome message
                    MessageBox.Show($"Welcome, {user.FirstName}!", "Login Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Redirect user based on role
                    switch (user.Role)
                    {
                        case "Admin":
                            new AdminWindow(_dbContext).Show();
                            break;
                        case "Staff":
                            break;
                        case "Encoder":
                            break;
                        default:
                            MessageBox.Show("Unauthorized role detected.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                    }

                    // Close login window after successful login
                    Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is MainWindow)?.Close();
                }
                else
                {
                    MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}
