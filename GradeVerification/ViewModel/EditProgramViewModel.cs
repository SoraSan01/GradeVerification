using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class EditProgramViewModel : INotifyPropertyChanged
    {
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
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private EditProgram _editWindow;  // Reference to the window

        private readonly Action _onUpdate; // Callback for UI refresh

        public EditProgramViewModel(AcademicProgram program, EditProgram editWindow, Action onUpdate)
        {
            _editWindow = editWindow;
            _onUpdate = onUpdate;

            Id = program.Id;
            ProgramCode = program.ProgramCode;
            ProgramName = program.ProgramName;

            SaveCommand = new RelayCommand(SaveProgram);
            CancelCommand = new RelayCommand(Cancel);
        }

        private async void SaveProgram(object obj)
        {
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

            _onUpdate?.Invoke(); // Notify the main view to refresh
            _editWindow.Close(); // Close window after saving
        }

        private void Cancel(object parameter)
        {
            // Close the window without saving
            Application.Current.Windows.OfType<EditProgram>().FirstOrDefault()?.Close();
        }
    }
}
