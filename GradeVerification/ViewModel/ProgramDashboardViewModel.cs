using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class ProgramDashboardViewModel : INotifyPropertyChanged
    {
        private string _searchText;
        private ObservableCollection<AcademicProgram> _programsMaster; // Master list
        private ObservableCollection<AcademicProgram> _filteredPrograms; // Filtered list

        private readonly Notifier _notifier;

        public ObservableCollection<AcademicProgram> Programs
        {
            get => _filteredPrograms;
            set { _filteredPrograms = value; OnPropertyChanged(nameof(Programs)); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterPrograms();
            }
        }

        public ICommand AddProgramCommand { get; }
        public ICommand EditProgramCommand { get; }
        public ICommand DeleteProgramCommand { get; }

        public ProgramDashboardViewModel()
        {
            _programsMaster = new ObservableCollection<AcademicProgram>();
            Programs = new ObservableCollection<AcademicProgram>();

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

            // Load programs on startup
            LoadProgramsAsync();

            AddProgramCommand = new RelayCommand(AddProgram);
            EditProgramCommand = new RelayCommand(EditProgram, param => param is AcademicProgram);
            DeleteProgramCommand = new RelayCommand(DeleteProgram, param => param is AcademicProgram);
        }

        private async void LoadProgramsAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var programs = await context.AcademicPrograms.ToListAsync();
                    _programsMaster = new ObservableCollection<AcademicProgram>(programs);

                    // Initially, set the filtered collection to the full list.
                    Programs = new ObservableCollection<AcademicProgram>(_programsMaster);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading programs: " + ex.Message);
                _notifier.ShowError("Error loading programs.");
            }
        }

        private void FilterPrograms()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // No search text: display all programs.
                Programs = new ObservableCollection<AcademicProgram>(_programsMaster);
            }
            else
            {
                var filtered = _programsMaster
                    .Where(p => p.ProgramCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                p.ProgramName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                Programs = new ObservableCollection<AcademicProgram>(filtered);
            }

            if (Programs.Count == 0)
            {
                _notifier.ShowError("No programs found matching your criteria.");
            }
        }

        private void AddProgram(object obj)
        {
            try
            {
                // Pass a context and a callback to refresh after adding.
                using (var context = new ApplicationDbContext())
                {
                    var addProgramWindow = new AddProgram(context, LoadProgramsAsync);
                    addProgramWindow.Show();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error adding program: " + ex.Message);
                _notifier.ShowError("Error opening add program window.");
            }
        }

        private void EditProgram(object obj)
        {
            if (obj is AcademicProgram selectedProgram)
            {
                var editWindow = new EditProgram();
                editWindow.DataContext = new EditProgramViewModel(selectedProgram, editWindow, LoadProgramsAsync);
                editWindow.ShowDialog();
            }
            else
            {
                _notifier.ShowError("Please select a program to edit.");
            }
        }

        private async void DeleteProgram(object obj)
        {
            if (obj is AcademicProgram program)
            {
                var result = MessageBox.Show($"Are you sure you want to delete {program.ProgramCode}?",
                    "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var context = new ApplicationDbContext())
                        {
                            var programToDelete = await context.AcademicPrograms.FindAsync(program.Id);
                            if (programToDelete != null)
                            {
                                context.AcademicPrograms.Remove(programToDelete);
                                await context.SaveChangesAsync();
                                _programsMaster.Remove(program);
                                FilterPrograms();
                            }
                        }
                        _notifier.ShowSuccess("Program deleted successfully!");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error deleting program: " + ex.Message);
                        _notifier.ShowError("Error deleting program.");
                    }
                }
            }
            else
            {
                _notifier.ShowError("No program selected for deletion.");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
