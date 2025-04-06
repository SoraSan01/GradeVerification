using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using GradeVerification.Commands;
using GradeVerification.Data;
using Microsoft.EntityFrameworkCore;
using ToastNotifications.Messages;

namespace GradeVerification.ViewModel
{
    public class ForgotPasswordViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationDbContext _dbContext;
        private string _email;
        private string _otpCode;
        private bool _isOtpEnabled;
        private string _generatedOtp;

        // Toast Notifications
        private readonly Notifier _notifier;

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string OtpCode
        {
            get => _otpCode;
            set { _otpCode = value; OnPropertyChanged(); }
        }

        public bool IsOtpEnabled
        {
            get => _isOtpEnabled;
            set { _isOtpEnabled = value; OnPropertyChanged(); }
        }

        public ICommand SendOtpCommand { get; set; }
        public ICommand VerifyOtpCommand { get; set; }
        public ICommand BackCommand { get; set; }

        public ForgotPasswordViewModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;

            SendOtpCommand = new RelayCommand(SendOtp);
            VerifyOtpCommand = new RelayCommand(VerifyOtp);
            BackCommand = new RelayCommand(Back);

            // Initialize Toast Notifications
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
        }

        private async void SendOtp(object parameter)
        {
            if (string.IsNullOrEmpty(Email) || !IsValidEmail(Email))
            {
                _notifier.ShowError("Please enter a valid email address.");
                return;
            }

            // Check if the email exists in the database
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == Email);

            if (user == null)
            {
                _notifier.ShowError("This email address is not registered.");
                return;
            }

            // Generate a random OTP
            _generatedOtp = GenerateOtp();

            // Send OTP via email
            bool isSent = SendOtpEmail(Email, _generatedOtp);

            if (isSent)
            {
                _notifier.ShowInformation("OTP has been sent to your email.");
                IsOtpEnabled = true;
            }
            else
            {
                _notifier.ShowError("Failed to send OTP. Please try again.");
            }
        }

        private void VerifyOtp(object parameter)
        {
            if (OtpCode == _generatedOtp)
            {
                _notifier.ShowSuccess("OTP verified successfully.");

                var changePass = new ChangePassword();
                changePass.DataContext = new ChangePasswordViewModel(_dbContext,Email);
                changePass.Show();
                Application.Current.Windows.OfType<ForgotPassword>().FirstOrDefault()?.Close();
            }
            else
            {
                _notifier.ShowError("Invalid OTP. Please try again.");
            }
        }

        private void Back(object parameter)
        {
            var login = new MainWindow(_dbContext);
            login.Show();
            Application.Current.Windows.OfType<ForgotPassword>().FirstOrDefault()?.Close();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateOtp()
        {
            Random random = new Random();
            int otp = random.Next(100000, 999999); // Generate a 6-digit OTP
            return otp.ToString();
        }

        private bool SendOtpEmail(string emailAddress, string otp)
        {
            try
            {
                // SMTP Configuration
                var smtpClient = new SmtpClient("smtp.gmail.com") // Example SMTP server
                {
                    Port = 587,
                    Credentials = new NetworkCredential("aujsceranreyven@gmail.com", "qdrmwtxwfojrjtal"),
                    EnableSsl = true,
                };

                // Prepare the message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("aujsceranreyven@gmail.com"),
                    Subject = "Your OTP Code for Password Reset",
                    Body = $"Your OTP code is: {otp}",
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(emailAddress);

                // Send the email
                smtpClient.Send(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                _notifier.ShowError($"Error sending OTP: {ex.Message}");
                return false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
