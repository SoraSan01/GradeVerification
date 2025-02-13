using GradeVerification.ViewModel;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GradeVerification.Data;

namespace GradeVerification.View.Admin.Windows
{
    public partial class AddUser : Window
    {
        private readonly ApplicationDbContext _dbContext;

        public AddUser(ApplicationDbContext dbContext)
        {
            InitializeComponent();
            _dbContext = dbContext;
            this.DataContext = new AddUserViewModel(dbContext);
        }

        // Allows window to be moved by dragging the top border
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddUserViewModel viewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddUserViewModel viewModel)
            {
                viewModel.ConfirmPassword = ((PasswordBox)sender).Password;
            }
        }

        public void ClearPasswordFields()
        {
            PasswordBoxControl.Clear();
            ConfirmPasswordBoxControl.Clear();
        }

        // Minimize window when the minimize button is clicked
        private void btn_Minimize(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Close window when the close button is clicked
        private void btn_Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
