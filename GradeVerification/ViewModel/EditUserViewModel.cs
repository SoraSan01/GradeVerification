using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class EditUserViewModel : INotifyPropertyChanged
    {
        private User _user;
        private readonly ApplicationDbContext _dbContext;

        public User User
        {
            get => _user;
            set { _user = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Roles { get; set; }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditUserViewModel(User user, ApplicationDbContext dbContext)
        {
            _user = user;
            _dbContext = dbContext;
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);

            Roles = new ObservableCollection<string> { "Admin", "Encoder", "Staff" };
        }

        private void Save(object parameter)
        {
            // Save changes to the database
            _dbContext.SaveChanges();
            // Close the window
            Application.Current.Windows.OfType<EditUser>().FirstOrDefault()?.Close();
        }

        private void Cancel(object parameter)
        {
            // Close the window without saving
            Application.Current.Windows.OfType<EditUser>().FirstOrDefault()?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
