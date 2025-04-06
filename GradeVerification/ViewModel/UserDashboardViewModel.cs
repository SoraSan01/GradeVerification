using GradeVerification.Commands;
using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace GradeVerification.ViewModel
{
    public class UserDashboardViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private Notifier _notifier;
        private ObservableCollection<User> _users;
        private string _searchText;
        private string _selectedRole;
        private User _selectedUser;
        private readonly ApplicationDbContext _dbContext;

        // CollectionView for filtering instead of recreating the collection
        public ICollectionView UsersView { get; private set; }

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
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    ValidateSearchText();
                    RefreshFilter();
                }
            }
        }

        // List of roles with a default "All Roles" option
        public List<string> Roles { get; } = new List<string> { "All Roles", "Admin", "Encoder" };

        public string SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (_selectedRole != value)
                {
                    _selectedRole = value;
                    OnPropertyChanged();
                    RefreshFilter();
                }
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
                    notificationLifetime: TimeSpan.FromSeconds(1.5),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(3));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });

            _dbContext = dbContext;
            LoadUsers();

            SelectedRole = "All Roles"; // Initialize the filter

            EditUserCommand = new RelayCommand(EditUser);
            DeleteUserCommand = new RelayCommand(DeleteUser);
            AddUserCommand = new RelayCommand(AddUser);
        }

        private void LoadUsers()
        {
            try
            {
                var usersFromDb = _dbContext.Users.ToList();
                Users = new ObservableCollection<User>(usersFromDb);
                // Set up the CollectionView for filtering
                UsersView = CollectionViewSource.GetDefaultView(Users);
                UsersView.Filter = FilterUsersPredicate;
            }
            catch (Exception ex)
            {
                ShowErrorNotification("Error loading users.");
                MessageBox.Show($"Error loading users: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool FilterUsersPredicate(object item)
        {
            if (item is User user)
            {
                bool matchesSearch = string.IsNullOrWhiteSpace(SearchText) ||
                                     user.FullName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesRole = string.IsNullOrWhiteSpace(SelectedRole) || SelectedRole == "All Roles" ||
                                   user.Role.Equals(SelectedRole, StringComparison.OrdinalIgnoreCase);
                return matchesSearch && matchesRole;
            }
            return false;
        }

        private void RefreshFilter()
        {
            UsersView?.Refresh();
        }

        private void AddUser(object parameter)
        {
            var addUserWindow = new AddUser();
            // Pass a callback to reload users after adding
            addUserWindow.DataContext = new AddUserViewModel(_dbContext, () =>
            {
                LoadUsers();
                RefreshFilter();
            });
            addUserWindow.Show();
        }

        private void EditUser(object parameter)
        {
            if (parameter is User selectedUser)
            {
                var editUserWindow = new EditUser();
                editUserWindow.DataContext = new EditUserViewModel(selectedUser, _dbContext, () =>
                {
                    LoadUsers();
                    RefreshFilter();
                });
                if (editUserWindow.ShowDialog() == true)
                {
                    LoadUsers(); // Refresh the list after editing
                    RefreshFilter();
                }
            }
            else
            {
                ShowErrorNotification("Invalid user selected for editing.");
            }
        }

        private void DeleteUser(object parameter)
        {
            if (parameter is User user)
            {
                var result = MessageBox.Show("Are you sure you want to delete this user?", "Confirm Deletion",
                                             MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _dbContext.Users.Remove(user);
                        _dbContext.SaveChanges();
                        Users.Remove(user);
                        ShowSuccessNotification("User deleted successfully.");
                    }
                    catch (Exception ex)
                    {
                        ShowErrorNotification("Error deleting user");
                        MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                ShowErrorNotification("No user selected to delete.");
            }
        }

        #region Validation (INotifyDataErrorInfo)

        private readonly Dictionary<string, List<string>> _propertyErrors = new Dictionary<string, List<string>>();

        public bool HasErrors => _propertyErrors.Any();
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return null;
            return _propertyErrors.ContainsKey(propertyName) ? _propertyErrors[propertyName] : null;
        }

        protected void AddError(string propertyName, string error)
        {
            if (!_propertyErrors.ContainsKey(propertyName))
            {
                _propertyErrors[propertyName] = new List<string>();
            }
            if (!_propertyErrors[propertyName].Contains(error))
            {
                _propertyErrors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        protected void ClearErrors(string propertyName)
        {
            if (_propertyErrors.ContainsKey(propertyName))
            {
                _propertyErrors.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }

        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        // A sample validation: you can adjust or add more as needed.
        private void ValidateSearchText()
        {
            ClearErrors(nameof(SearchText));
            if (!string.IsNullOrEmpty(SearchText) && SearchText.Length > 100)
            {
                AddError(nameof(SearchText), "Search text is too long (maximum 100 characters).");
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Notifications

        private void ShowSuccessNotification(string message)
        {
            _notifier.ShowSuccess(message);
        }

        private void ShowErrorNotification(string message)
        {
            _notifier.ShowError(message);
        }

        #endregion
    }
}
