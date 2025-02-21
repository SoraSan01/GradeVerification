using GradeVerification.Commands;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class SettingsDashboardViewModel : INotifyPropertyChanged
    {
        public ICommand BackupCommand { get; }
        public ICommand RestoreCommand { get; }

        public SettingsDashboardViewModel()
        {
            BackupCommand = new RelayCommand(_ => BackupData());
            RestoreCommand = new RelayCommand(_ => RestoreData());
        }

        private void BackupData()
        {
            // Let the user choose where to save the backup file.
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*",
                // Optionally set an initial directory that has proper permissions.
                InitialDirectory = @"C:\GradeVerification"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string backupFilePath = saveFileDialog.FileName;
                try
                {
                    // Use your actual connection string and database name.
                    string connectionString = "Data Source=DESKTOP-QRF1854\\SQLEXPRESS;Initial Catalog=GradeVerification;Integrated Security=True;TrustServerCertificate=True;";
                    string databaseName = "GradeVerification";

                    // SQL command to backup the database
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

                    MessageBox.Show("Backup completed successfully!", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error during backup: " + ex.Message, "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RestoreData()
        {
            // Let the user select a backup file.
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string backupFilePath = openFileDialog.FileName;
                try
                {
                    // When restoring, connect to the master database.
                    string connectionString = "Data Source=DESKTOP-QRF1854\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;";
                    string databaseName = "GradeVerification";

                    // Set the database to single-user mode, restore the database, and then revert to multi-user mode.
                    string setSingleUser = $@"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
                    string restoreCommandText = $@"RESTORE DATABASE [{databaseName}] FROM DISK = '{backupFilePath}' WITH REPLACE;";
                    string setMultiUser = $@"ALTER DATABASE [{databaseName}] SET MULTI_USER;";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        SqlCommand command = new SqlCommand(setSingleUser + restoreCommandText + setMultiUser, connection);
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    MessageBox.Show("Restore completed successfully!", "Restore", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error during restore: " + ex.Message, "Restore Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
