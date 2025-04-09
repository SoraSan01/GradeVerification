using GradeVerification.Commands;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class SettingsDashboardViewModel : INotifyPropertyChanged
    {
        private readonly Notifier _notifier;
        private readonly DispatcherTimer _autoBackupTimer;
        private const string BackupFolderPath = @"C:\GradeVerification";
        // Set the auto backup interval as needed (e.g., 30 minutes)
        private readonly TimeSpan _backupInterval = TimeSpan.FromHours(1);

        public ICommand BackupCommand { get; }
        public ICommand RestoreCommand { get; }
        public ICommand AutoBackupCommand { get; }  // If you want a manual trigger for auto backup

        public SettingsDashboardViewModel()
        {
            // Configure the notifier
            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(100));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            BackupCommand = new RelayCommand(_ => AutoBackupData());
            RestoreCommand = new RelayCommand(_ => RestoreData());

            // Start the auto backup timer.
            _autoBackupTimer = new DispatcherTimer();
            _autoBackupTimer.Interval = _backupInterval;
            _autoBackupTimer.Tick += AutoBackupTimer_Tick;
            _autoBackupTimer.Start();

            // (Optional) Create backup folder if it doesn't exist
            EnsureBackupFolderExists();
        }

        private void AutoBackupTimer_Tick(object sender, EventArgs e)
        {
            AutoBackupData();
        }

        // Checks if the backup folder exists; creates it if not.
        private void EnsureBackupFolderExists()
        {
            if (!Directory.Exists(BackupFolderPath))
            {
                Directory.CreateDirectory(BackupFolderPath);
            }
        }

        // Auto backup that is triggered by the timer.
        private void AutoBackupData()
        {
            try
            {
                EnsureBackupFolderExists();
                // Generate a backup file name based on timestamp.
                string backupFilePath = Path.Combine(BackupFolderPath, $"GradeVerificationBackup_{DateTime.Now:yyyyMMddHHmmss}.bak");
                PerformBackup(backupFilePath);
                _notifier.ShowSuccess("Backup completed successfully!");
            }
            catch (Exception ex)
            {
                _notifier.ShowError("Error during auto backup: " + ex.Message);
            }
        }

        // Performs the backup using the specified file path.
        private void PerformBackup(string backupFilePath)
        {
            try
            {
                // Use your actual connection string and database name.
                string connectionString = "Data Source=DESKTOP-QRF1854\\SQLEXPRESS;Initial Catalog=GradeVerification;Integrated Security=True;TrustServerCertificate=True;";
                string databaseName = "GradeVerification";

                string backupCommandText = $@"
                    BACKUP DATABASE [{databaseName}]
                    TO DISK = '{backupFilePath}'
                    WITH INIT, FORMAT;
                ";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(backupCommandText, connection);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Backup operation failed: " + ex.Message);
            }
        }

        private void RestoreData()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string backupFilePath = openFileDialog.FileName;
                try
                {
                    string connectionString = "Data Source=DESKTOP-QRF1854\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;";
                    string databaseName = "GradeVerification";

                    string setSingleUser = $@"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
                    string restoreCommandText = $@"RESTORE DATABASE [{databaseName}] FROM DISK = '{backupFilePath}' WITH REPLACE;";
                    string setMultiUser = $@"ALTER DATABASE [{databaseName}] SET MULTI_USER;";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        SqlCommand command = new SqlCommand(setSingleUser + restoreCommandText + setMultiUser, connection);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    _notifier.ShowSuccess("Restore completed successfully!");
                }
                catch (Exception ex)
                {
                    _notifier.ShowError("Error during restore: " + ex.Message);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
