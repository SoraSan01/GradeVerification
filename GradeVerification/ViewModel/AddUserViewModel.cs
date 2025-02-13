using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GradeVerification.Data;
using GradeVerification.Model;
using Microsoft.EntityFrameworkCore;
using GradeVerification.Commands;
using GradeVerification.Service;
using GradeVerification.View.Admin.Windows;
using System.Text;
using System.Security.Cryptography;

namespace GradeVerification.ViewModel
{
    public class AddUserViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserService _userService;

        private string _firstName;
        private string _lastName;
        private string _username;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _selectedRole;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public ObservableCollection<string> Roles { get; set; } = new ObservableCollection<string> { "Admin", "Staff", "Encoder" };

        public ICommand SaveUserCommand { get; }

        public AddUserViewModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _userService = new UserService(dbContext);
            SaveUserCommand = new RelayCommand(async _ => await SaveUserAsync());
        }

        private async Task SaveUserAsync()
        {
            if (string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName) ||
                string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(ConfirmPassword) ||
                string.IsNullOrWhiteSpace(SelectedRole))
            {
                MessageBox.Show("Please fill in all fields!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Password != ConfirmPassword)
            {
                MessageBox.Show("Passwords do not match!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            MessageBox.Show("Username already exists!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                        return false;
                    }

                    // Generate unique User ID
                    string newUserId = await _userService.GenerateUserIdAsync();

                    // Hash the password before storing
                    string hashedPassword = HashPassword(Password);

                    var newUser = new User
                    {
                        Id = newUserId,  // Assign generated ID
                        FirstName = FirstName,
                        LastName = LastName,
                        Username = Username,
                        Email = Email,
                        Password = hashedPassword, // Store the hashed password
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
                MessageBox.Show("User successfully added!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearFields();
            }
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

            // Notify the View to clear the PasswordBoxes
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
        }
    }
}
