using GradeVerification.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using GradeVerification.Model;
using GradeVerification.Commands;
using GradeVerification.Model;
using GradeVerification.Data;
using GradeVerification.View.Admin.Windows;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;
using GradeVerification.Service;

namespace GradeVerification.ViewModel
{
    public class AddProgramViewModel : INotifyPropertyChanged
    {

        private Notifier _notifier;
        private readonly Action _onUpdate;
        private readonly ActivityLogService _activityLogService;

        private string _programCode;
        private string _programName;

        public string ProgramCode
        {
            get => _programCode;
            set
            {
                _programCode = value;
                OnPropertyChanged(nameof(ProgramCode));
            }
        }

        public string ProgramName
        {
            get => _programName;
            set
            {
                _programName = value;
                OnPropertyChanged(nameof(ProgramName));
            }
        }

        public ICommand SaveProgramCommand { get; }
        public ICommand CancelCommand { get; }
        public string Error => null;

        private readonly ApplicationDbContext _dbContext;

        public AddProgramViewModel(ApplicationDbContext dbContext, Action onUpdate)
        {
            _activityLogService = new ActivityLogService();

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
            _onUpdate = onUpdate;

            SaveProgramCommand = new RelayCommand(SaveProgram, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(ProgramCode):
                        if (string.IsNullOrWhiteSpace(ProgramCode))
                            return "Program Code is required.";
                        break;
                    case nameof(ProgramName):
                        if (string.IsNullOrWhiteSpace(ProgramName))
                            return "Program Name is required.";
                        break;
                }
                return null;
            }
        }

        private bool CanSave(object parameter)
        {
            return !string.IsNullOrWhiteSpace(ProgramCode) && !string.IsNullOrWhiteSpace(ProgramName);
        }

        private void SaveProgram(object parameter)
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    // Get the last ID from the database
                    var lastId = context.AcademicPrograms
                                        .OrderByDescending(p => p.Id)
                                        .Select(p => p.Id)
                                        .FirstOrDefault();

                    // Create new program
                    var program = new AcademicProgram
                    {
                        ProgramCode = this.ProgramCode,
                        ProgramName = this.ProgramName
                    };

                    // Generate new ID
                    program.GenerateNewId(lastId);

                    // Save to database
                    context.AcademicPrograms.Add(program);
                    context.SaveChanges();

                    string currentUsername = Environment.UserName;

                    _activityLogService.LogActivity("Program", $"Program Added by {currentUsername}", "Book");
                }

                ShowSuccessNotification("Program Saved!");

                // Reset fields after saving
                ProgramCode = string.Empty;
                ProgramName = string.Empty;

                _onUpdate?.Invoke(); // Notify main view to refresh UI
            }
            catch (Exception ex)
            {
                ShowErrorNotification("Error saving program");
                MessageBox.Show($"Error saving program: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel(object parameter)
        {
            Application.Current.Windows.OfType<AddProgram>().FirstOrDefault()?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
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

