using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using GradeVerification.Commands;       // Assuming your RelayCommand implementation is here
using GradeVerification.Data;           // Your ApplicationDbContext location
using GradeVerification.Model;          // Your Professor model location

namespace GradeVerification.ViewModel
{
    public class ManageProfessorViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationDbContext _context;
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
        public ICommand CloseCommand { get; } // You can bind this to a window close action if desired.

        public ManageProfessorViewModel(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Professors = new ObservableCollection<Professor>();

            // Initialize commands
            AddProfessorCommand = new RelayCommand(async (o) => await AddProfessorAsync(), (o) => CanAddProfessor());
            DeleteProfessorCommand = new RelayCommand(async (o) => await DeleteProfessorAsync(), (o) => CanDeleteProfessor());

            LoadProfessors(); // Ensure professors are loaded on initialization
        }

        private void LoadProfessors()
        {
            // Load the professors from the database into the ObservableCollection.
            Professors.Clear();
            var professors = _context.Professors.ToList();
            foreach (var professor in professors)
            {
                Professors.Add(professor);
            }
        }

        private async Task AddProfessorAsync()
        {
            if (string.IsNullOrWhiteSpace(NewProfessorInput))
                return;

            // Create a new professor record.
            var professor = new Professor
            {
                Name = NewProfessorInput.Trim()
            };

            _context.Professors.Add(professor);
            await _context.SaveChangesAsync();

            // Add to the ObservableCollection to refresh the UI.
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

            // Remove from the DbContext.
            _context.Professors.Remove(SelectedProfessor);
            await _context.SaveChangesAsync();

            // Remove from the ObservableCollection.
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
