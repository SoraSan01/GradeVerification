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

namespace GradeVerification.ViewModel
{
    public class AddProgramViewModel : INotifyPropertyChanged
    {
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

        private readonly ApplicationDbContext _dbContext;
        private readonly Action _onUpdate; // Callback to refresh the program list

        public AddProgramViewModel(ApplicationDbContext dbContext, Action onUpdate)
        {
            _dbContext = dbContext;
            _onUpdate = onUpdate;

            SaveProgramCommand = new RelayCommand(SaveProgram, CanSave);
            CancelCommand = new RelayCommand(Cancel);
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
                }

                MessageBox.Show($"Program Saved!\nCode: {ProgramCode}\nName: {ProgramName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reset fields after saving
                ProgramCode = string.Empty;
                ProgramName = string.Empty;

                _onUpdate?.Invoke(); // Notify main view to refresh UI
            }
            catch (Exception ex)
            {
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
    }
}

