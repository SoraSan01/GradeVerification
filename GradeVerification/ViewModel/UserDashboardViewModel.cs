using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace GradeVerification.ViewModel
{
    public class UserDashboardViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<User> _users;
        private string _searchText;
        private string _selectedRole;
        private User _selectedUser;
        private readonly ApplicationDbContext _dbContext;

        public ObservableCollection<User> Users
        {
            get => _users;
            set { _users = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterUsers(); }
        }

        public string SelectedRole
        {
            get => _selectedRole;
            set { _selectedRole = value; OnPropertyChanged(); FilterUsers(); }
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set { _selectedUser = value; OnPropertyChanged(); }
        }

        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }

        // Inject ApplicationDbContext into ViewModel
        public UserDashboardViewModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            LoadUsers();  // Fetch users from database

            EditUserCommand = new RelayCommand(EditUser);
            DeleteUserCommand = new RelayCommand(DeleteUser);
        }

        private void LoadUsers()
        {
            var usersFromDb = _dbContext.Users.ToList();  // Fetch all users from database
            Users = new ObservableCollection<User>(usersFromDb);
        }

        private void EditUser(object parameter)
        {
            if (parameter is User selectedUser)
            {
                var editUser = new EditUser(selectedUser, _dbContext);
                if (editUser.ShowDialog() == true)
        {
                    LoadUsers(); // Refresh the list after editing
                }
            }
        }

        private void DeleteUser(object parameter)
        {
            if (parameter is User user)
            {
                _dbContext.Users.Remove(user);
                _dbContext.SaveChanges(); // Remove from database

                Users.Remove(user); // Update UI
            }
        }

        private void FilterUsers()
        {
            var filteredUsers = _dbContext.Users.AsQueryable();

            if (!string.IsNullOrEmpty(SearchText))
                filteredUsers = filteredUsers.Where(u => u.FullName.Contains(SearchText));

            if (!string.IsNullOrEmpty(SelectedRole) && SelectedRole != "Filter by Role")
                filteredUsers = filteredUsers.Where(u => u.Role == SelectedRole);

            Users = new ObservableCollection<User>(filteredUsers.ToList());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
