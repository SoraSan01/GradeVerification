using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class ManageSchoolYearsViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationDbContext _context;
        private string _newYearInput;
        private SchoolYear _selectedSchoolYear;

        public ManageSchoolYearsViewModel()
        {
            _context = new ApplicationDbContext();
            SchoolYears = new ObservableCollection<SchoolYear>();

            LoadSchoolYearsCommand = new RelayCommand(async _ => await LoadSchoolYears());
            AddSchoolYearCommand = new RelayCommand(async _ => await AddSchoolYear());
            DeleteSchoolYearCommand = new RelayCommand(async _ => await DeleteSchoolYear(), CanDeleteSchoolYear);
            CloseCommand = new RelayCommand(_ => CloseWindow());

            // Initial load
            LoadSchoolYearsCommand.Execute(null);
        }

        public ObservableCollection<SchoolYear> SchoolYears { get; private set; }

        public string NewYearInput
        {
            get => _newYearInput;
            set
            {
                _newYearInput = value;
                OnPropertyChanged(nameof(NewYearInput));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public SchoolYear SelectedSchoolYear
        {
            get => _selectedSchoolYear;
            set
            {
                _selectedSchoolYear = value;
                OnPropertyChanged(nameof(SelectedSchoolYear));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand LoadSchoolYearsCommand { get; }
        public ICommand AddSchoolYearCommand { get; }
        public ICommand DeleteSchoolYearCommand { get; }
        public ICommand CloseCommand { get; }

        private async Task LoadSchoolYears()
        {
            try
            {
                var years = await _context.SchoolYears
                    .OrderByDescending(y => y.SchoolYears)
                    .ToListAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SchoolYears.Clear();
                    foreach (var year in years)
                    {
                        SchoolYears.Add(year);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading school years: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddSchoolYear()
        {
            if (!IsValidYearFormat(NewYearInput))
            {
                MessageBox.Show("Invalid year format. Please use YYYY-YYYY format.",
                    "Invalid Format", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var newYear = new SchoolYear { SchoolYears = NewYearInput.Trim() };

                if (_context.SchoolYears.Any(y => y.SchoolYears == newYear.SchoolYears))
                {
                    MessageBox.Show("This school year already exists.", "Duplicate Entry",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _context.SchoolYears.AddAsync(newYear);
                await _context.SaveChangesAsync();

                SchoolYears.Insert(0, newYear);
                NewYearInput = string.Empty;

                MessageBox.Show("School year added successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding school year: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteSchoolYear()
        {
            if (SelectedSchoolYear == null) return;

            var confirm = MessageBox.Show($"Are you sure you want to delete '{SelectedSchoolYear.SchoolYears}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                // Check if year is in use
                var isUsed = await _context.Students
                    .AnyAsync(s => s.Year == SelectedSchoolYear.SchoolYears);

                if (isUsed)
                {
                    MessageBox.Show("Cannot delete school year that is in use by students.",
                        "In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _context.SchoolYears.Remove(SelectedSchoolYear);
                await _context.SaveChangesAsync();

                SchoolYears.Remove(SelectedSchoolYear);
                SelectedSchoolYear = null;

                MessageBox.Show("School year deleted successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting school year: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanDeleteSchoolYear(object parameter) =>
            SelectedSchoolYear != null;

        private bool IsValidYearFormat(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            var parts = input.Split('-');
            return parts.Length == 2 &&
                   parts[0].Length == 4 &&
                   parts[1].Length == 4 &&
                   int.TryParse(parts[0], out int start) &&
                   int.TryParse(parts[1], out int end) &&
                   end == start + 1;
        }

        private void CloseWindow()
        {
            Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this)?
                .Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}