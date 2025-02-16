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
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class UserDashboardViewModel : INotifyPropertyChanged
    {
        private Notifier _notifier;

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
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterUsers(); // Trigger filtering when SearchText changes
            }
        }

        // Define the list of roles
        public List<string> Roles { get; } = new List<string> { "All Roles", "Admin", "Encoder", "Staff" };

        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                OnPropertyChanged();
                FilterUsers(); // Trigger filtering when SelectedRole changes
            }
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set { _selectedUser = value; OnPropertyChanged(); }
        }

        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }

        public UserDashboardViewModel(ApplicationDbContext dbContext)
        {

            _notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(
                    corner: Corner.BottomRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            _dbContext = dbContext;
            LoadUsers();

            SelectedRole = "All Roles"; // Initialize SelectedRole to "All Roles"

            EditUserCommand = new RelayCommand(EditUser);
            DeleteUserCommand = new RelayCommand(DeleteUser);
            AddUserCommand = new RelayCommand(AddUser);

        }

        private void LoadUsers()
        {
            var usersFromDb = _dbContext.Users.ToList();
            Users = new ObservableCollection<User>(usersFromDb);
        }

        private void FilterUsers()
        {
            var filteredUsers = _dbContext.Users.AsEnumerable(); // Use AsEnumerable to work in memory

            if (!string.IsNullOrEmpty(SearchText))
            {
                filteredUsers = filteredUsers.Where(u => u.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(SelectedRole) && SelectedRole != "All Roles")
            {
                filteredUsers = filteredUsers.Where(u => u.Role.Equals(SelectedRole, StringComparison.OrdinalIgnoreCase));
            }

            Users = new ObservableCollection<User>(filteredUsers.ToList());
        }

        private void AddUser(object parameter)
        {
            var addUser = new AddUser();
            addUser.DataContext = new AddUserViewModel(_dbContext, LoadUsers);
            addUser.Show();
        }
        private void EditUser(object parameter)
        {
            if (parameter is User selectedUser)
            {
                var editUser = new EditUser();
                editUser.DataContext = new EditUserViewModel(selectedUser, _dbContext, LoadUsers);
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
                // Confirm the deletion action from the user
                var result = MessageBox.Show("Are you sure you want to delete this user?", "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Remove the user from the database
                        _dbContext.Users.Remove(user);
                        _dbContext.SaveChanges();

                        // Remove the user from the observable collection
                        Users.Remove(user);

                        // Optionally, show a success message
                        ShowSuccessNotification("User deleted successfully.");
                    }
                    catch (Exception ex)
                    {
                        ShowErrorNotification("Error deleting user");
                        MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ShowSuccessNotification(string message)
        {
            _notifier.ShowSuccess(message);
        }

        private void ShowErrorNotification(string message)
        {
            _notifier.ShowError(message);
        }
    }
}