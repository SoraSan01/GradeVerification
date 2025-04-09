using GradeVerification.Commands;
using GradeVerification.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;
using Application = System.Windows.Application;

namespace GradeVerification.ViewModel
{
    public class ChangePasswordViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationDbContext _dbContext;
        private string _newPassword;
        private string _confirmPassword;
        public string Email { get; set; }

        // Toast Notifications
        private readonly Notifier _notifier;

        public string NewPassword
        {
            get => _newPassword;
            set { _newPassword = value; OnPropertyChanged(); }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(); }
        }

        public ICommand ChangePasswordCommand { get; set; }
        public ICommand BackCommand { get; set; }

        public ChangePasswordViewModel(ApplicationDbContext dbContext, string email)
        {
            _dbContext = dbContext;
            ChangePasswordCommand = new RelayCommand(ChangePassword);
            BackCommand = new RelayCommand(Back);

            Email = email;

            // Initialize Toast Notifications
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
        }

        private async void ChangePassword(object parameter)
        {
            if (string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
            {
                ShowErrorNotification("Please enter both passwords.");
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                ShowErrorNotification("Passwords do not match.");
                return;
            }

            // Retrieve the user (assuming you're storing the user information)
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == Email); // Email should be set by the previous view

            if (user == null)
            {
                ShowErrorNotification("User not found.");

                return;
            }

            // Hash the new password
            string newPasswordHash = HashPassword(NewPassword);

            // Update the password (ensure proper hashing)
            user.Password = newPasswordHash;  // Use the hashed password here

            await _dbContext.SaveChangesAsync();
            ShowSuccessNotification("Password changed successfully.");

            var login = new MainWindow(_dbContext);
            login.Show();
            Application.Current.Windows.OfType<ChangePassword>().FirstOrDefault()?.Close();
        }

        private void Back(object parameter)
        {
            var login = new MainWindow(_dbContext);
            login.Show();
            Application.Current.Windows.OfType<ChangePassword>().FirstOrDefault()?.Close();
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Compute the hash of the password
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert the byte array to a Base64 string (to match the verification method)
                return Convert.ToBase64String(bytes);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
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
