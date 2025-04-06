using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using ToastNotifications.Messages;
using GradeVerification.Service;

namespace GradeVerification.ViewModel
{
    public class EditProgramViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Notifier _notifier;

        private readonly ActivityLogService _activityLogService;

        private string _programCode;
        private string _programName;

        public string Id { get; set; }  // Store the program ID

        public string ProgramCode
        {
            get => _programCode;
            set { _programCode = value; OnPropertyChanged(nameof(ProgramCode)); }
        }

        public string ProgramName
        {
            get => _programName;
            set { _programName = value; OnPropertyChanged(nameof(ProgramName)); }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            // Refresh the state of the Save command when a property changes.
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // IDataErrorInfo implementation for inline validation.
        public string Error => null;
        public string this[string columnName]
        {
            get
            {
                string error = null;
                switch (columnName)
                {
                    case nameof(ProgramCode):
                        if (string.IsNullOrWhiteSpace(ProgramCode))
                            error = "Program Code is required.";
                        else if (!ProgramCode.All(char.IsLetterOrDigit))
                            error = "Program Code must be alphanumeric.";
                        break;
                    case nameof(ProgramName):
                        if (string.IsNullOrWhiteSpace(ProgramName))
                            error = "Program Name is required.";
                        break;
                }
                return error;
            }
        }

        // Helper method to determine if any validation errors exist.
        private bool HasErrors() =>
            !string.IsNullOrEmpty(this[nameof(ProgramCode)]) ||
            !string.IsNullOrEmpty(this[nameof(ProgramName)]);

        private EditProgram _editWindow;  // Reference to the window
        private readonly Action _onUpdate; // Callback for UI refresh

        public EditProgramViewModel(AcademicProgram program, EditProgram editWindow, Action onUpdate)
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

            _editWindow = editWindow;
            _onUpdate = onUpdate;

            Id = program.Id;
            ProgramCode = program.ProgramCode;
            ProgramName = program.ProgramName;

            // Create the command with a CanExecute predicate that checks for errors.
            SaveCommand = new RelayCommand(SaveProgram, _ => !HasErrors());
            CancelCommand = new RelayCommand(Cancel);
        }

        private async void SaveProgram(object obj)
        {
            // Final validation check.
            if (HasErrors())
            {
                ShowErrorNotification("Please correct the errors before saving.");
                return;
            }

            using (var context = new ApplicationDbContext())
            {
                var programToUpdate = await context.AcademicPrograms.FindAsync(Id);
                if (programToUpdate != null)
                {
                    programToUpdate.ProgramCode = ProgramCode;
                    programToUpdate.ProgramName = ProgramName;
                    await context.SaveChangesAsync();
                }
            }

            ShowSuccessNotification("Program Updated Successfully!");

            string currentUsername = Environment.UserName;

            _activityLogService.LogActivity("Program", "Edit", $"Program Edited by {currentUsername}");

            _onUpdate?.Invoke(); // Notify the main view to refresh.
            _editWindow.Close(); // Close the window after saving.
        }

        private void Cancel(object parameter)
        {
            // Close the window without saving.
            Application.Current.Windows.OfType<EditProgram>().FirstOrDefault()?.Close();
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
