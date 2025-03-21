using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GradeVerification.Commands;       // Assuming your RelayCommand implementation is here
using GradeVerification.Data;           // Your ApplicationDbContext location
using GradeVerification.Model;

namespace GradeVerification.ViewModel
{
    public class ManageProfessorViewModel : INotifyPropertyChanged
    {
        private string _newProfessorInput;
        private Professor _selectedProfessor;

        public ObservableCollection<Professor> Professors { get; set; }

        public string NewProfessorInput
        {
            get => _newProfessorInput;
            set
            {
                _newProfessorInput = value;
                OnPropertyChanged();
                // Notify that the Add command's CanExecute might have changed.
                (AddProfessorCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public Professor SelectedProfessor
        {
            get => _selectedProfessor;
            set
            {
                _selectedProfessor = value;
                OnPropertyChanged();
                // Notify that the Delete command's CanExecute might have changed.
                (DeleteProfessorCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // Commands
        public ICommand AddProfessorCommand { get; }
        public ICommand DeleteProfessorCommand { get; }
        public ICommand CloseCommand { get; } // Bind this to a window close action if desired.

        public ManageProfessorViewModel()
        {
            Professors = new ObservableCollection<Professor>();

            LoadProfessors(); // Load professors on initialization

            // Initialize commands
            AddProfessorCommand = new RelayCommand(async (o) => await AddProfessorAsync(), (o) => CanAddProfessor());
            DeleteProfessorCommand = new RelayCommand(async (o) => await DeleteProfessorAsync(), (o) => CanDeleteProfessor());

        }

        private void LoadProfessors()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    // Clear the collection and load professors from the database.
                    Professors.Clear();
                    var professors = context.Professors.ToList();

                    foreach (var professor in professors)
                    {
                        Professors.Add(professor);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading professors: " + ex.Message);
            }
        }

        private async Task AddProfessorAsync()
        {
            if (string.IsNullOrWhiteSpace(NewProfessorInput))
                return;

            // Create a new professor record using a fresh DbContext instance.
            Professor professor;
            using (var context = new ApplicationDbContext())
            {
                professor = new Professor
                {
                    Name = NewProfessorInput.Trim()
                };

                context.Professors.Add(professor);
                await context.SaveChangesAsync();
            }

            // Update the UI-bound collection.
            Professors.Add(professor);
            NewProfessorInput = string.Empty;
        }

        private bool CanAddProfessor()
        {
            return !string.IsNullOrWhiteSpace(NewProfessorInput);
        }

        private async Task DeleteProfessorAsync()
        {
            if (SelectedProfessor == null)
                return;

            // Remove using a fresh DbContext instance.
            using (var context = new ApplicationDbContext())
            {
                // Attach if not already tracked by this context.
                context.Professors.Attach(SelectedProfessor);
                context.Professors.Remove(SelectedProfessor);
                await context.SaveChangesAsync();
            }

            // Update the ObservableCollection.
            Professors.Remove(SelectedProfessor);
            SelectedProfessor = null;
        }

        private bool CanDeleteProfessor()
        {
            return SelectedProfessor != null;
        }

        // INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
