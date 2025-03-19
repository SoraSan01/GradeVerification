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
    public class ManageYearLevelViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationDbContext _context;

        // The collection that is bound to the ListBox in the view.
        public ObservableCollection<YearLevel> YearLevels { get; set; }

        private string _newYearLevelInput;
        public string NewYearLevelInput
        {
            get => _newYearLevelInput;
            set
            {
                _newYearLevelInput = value;
                OnPropertyChanged(nameof(NewYearLevelInput));
                // Notify the command that its executability may have changed.
                (AddYearLevelCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private YearLevel _selectedYearLevel;
        public YearLevel SelectedYearLevel
        {
            get => _selectedYearLevel;
            set
            {
                _selectedYearLevel = value;
                OnPropertyChanged(nameof(SelectedYearLevel));
                (DeleteYearLevelCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand AddYearLevelCommand { get; }
        public ICommand DeleteYearLevelCommand { get; }

        public ManageYearLevelViewModel()
        {
            _context = new ApplicationDbContext();
            YearLevels = new ObservableCollection<YearLevel>();

            // Initialize commands with execute and can execute predicates.
            AddYearLevelCommand = new RelayCommand(async o => await AddYearLevelAsync(),
                                                     o => !string.IsNullOrWhiteSpace(NewYearLevelInput));
            DeleteYearLevelCommand = new RelayCommand(async o => await DeleteYearLevelAsync(),
                                                        o => SelectedYearLevel != null);

            LoadYearLevels();
        }

        /// <summary>
        /// Loads the list of year levels from the database.
        /// </summary>
        private async void LoadYearLevels()
        {
            try
            {
                var levels = await _context.YearLevels
                                           .OrderBy(y => y.LevelName)
                                           .ToListAsync();

                YearLevels.Clear();
                foreach (var level in levels)
                {
                    YearLevels.Add(level);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading year levels: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Adds a new year level to the database.
        /// </summary>
        private async Task AddYearLevelAsync()
        {
            if (string.IsNullOrWhiteSpace(NewYearLevelInput))
                return;

            try
            {
                var newLevel = new YearLevel { LevelName = NewYearLevelInput };
                _context.YearLevels.Add(newLevel);
                await _context.SaveChangesAsync();

                // Refresh the collection and clear the input.
                LoadYearLevels();
                NewYearLevelInput = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding year level: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Deletes the selected year level from the database.
        /// </summary>
        private async Task DeleteYearLevelAsync()
        {
            if (SelectedYearLevel == null)
                return;

            try
            {
                _context.YearLevels.Remove(SelectedYearLevel);
                await _context.SaveChangesAsync();

                // Refresh the collection.
                LoadYearLevels();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting year level: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
