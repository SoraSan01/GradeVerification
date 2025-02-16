using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Helper;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GradeVerification.ViewModel
{
    public class ShowGradeViewModel : INotifyPropertyChanged
    {
        private readonly Student _student;
        private readonly ApplicationDbContext _context;

        public Student Student => _student;
        public ObservableCollection<Grade> Grades { get; set; } = new ObservableCollection<Grade>();

        private string _currentDate;
        public string CurrentDate
        {
            get { return _currentDate; }
            set
            {
                _currentDate = value;
                OnPropertyChanged(nameof(CurrentDate));
            }
        }

        public ICommand PrintCommand { get; }

        public ICommand CloseCommand { get; }

        public ShowGradeViewModel(Student student)
        {
            _student = student;
            _context = new ApplicationDbContext();
            CloseCommand = new RelayCommand(CloseWindow);
            PrintCommand = new RelayCommand(PrintWindow);

            CurrentDate = DateTime.Now.ToString("MMMM dd, yyyy");

            LoadGrades();
        }

        private async void LoadGrades()
        {
            var grades = await _context.Grade
                .Include(g => g.Subject)
                .Where(g => g.StudentId == _student.Id)
                .ToListAsync();

            foreach (var grade in grades)
            {
                Grades.Add(grade);
            }
        }

        private void PrintWindow(object parameter)
        {
            if (parameter is not UIElement printArea)
                return;

            PrintDialog printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                // Temporarily hide buttons
                var buttons = printArea.FindVisualChildren<Button>();
                foreach (var button in buttons)
                {
                    button.Visibility = Visibility.Collapsed;
                }

                printDialog.PrintVisual(printArea, "Printing Student Grades");

                // Restore buttons
                foreach (var button in buttons)
                {
                    button.Visibility = Visibility.Visible;
                }
            }
        }

        private void CloseWindow(object parameter)
        {
            Application.Current.Windows.OfType<ShowGradeWindow>().FirstOrDefault()?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
