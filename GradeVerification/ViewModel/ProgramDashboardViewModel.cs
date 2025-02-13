using GradeVerification.Model;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using GradeVerification.Data;
using GradeVerification.Commands;
using GradeVerification.View.Admin.Windows;
using System.Windows;

namespace GradeVerification.ViewModel
{
    public class ProgramDashboardViewModel : INotifyPropertyChanged
    {
        private string _searchText;
        private ObservableCollection<AcademicProgram> _programs;
        private ObservableCollection<AcademicProgram> _filteredPrograms;

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
            _programs = new ObservableCollection<AcademicProgram>();
            Programs = new ObservableCollection<AcademicProgram>();

            LoadProgramsAsync();  // Load data from database on startup

            AddProgramCommand = new RelayCommand(AddProgram);
            EditProgramCommand = new RelayCommand(EditProgram);
            DeleteProgramCommand = new RelayCommand(DeleteProgram);
        }

        private async void LoadProgramsAsync()
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var programs = await context.AcademicPrograms.ToListAsync();
                    Programs = new ObservableCollection<AcademicProgram>(programs);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading programs: " + ex.Message);
            }
        }

        private void FilterPrograms()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Programs = new ObservableCollection<AcademicProgram>(_programs);
            }
            else
            {
                Programs = new ObservableCollection<AcademicProgram>(_programs
                    .Where(p => p.ProgramCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                p.ProgramName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
            }
        }

        private void AddProgram(object obj)
        {
            try
            {
                using (var context = new ApplicationDbContext())
                {
                    AddProgram addProgram = new AddProgram(context, LoadProgramsAsync);
                    addProgram.Show();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading programs: " + ex.Message);
            }
        }

        private void EditProgram(object obj)
        {
            if (obj is AcademicProgram selectedProgram)
            {
                var editWindow = new EditProgram();
                var editViewModel = new EditProgramViewModel(selectedProgram, editWindow, LoadProgramsAsync);
                editWindow.DataContext = editViewModel;
                editWindow.ShowDialog();
            }
        }


        private async void DeleteProgram(object obj)
        {
            if (obj is AcademicProgram program)
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

                            _programs.Remove(program);
                            FilterPrograms();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error deleting program: " + ex.Message);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
