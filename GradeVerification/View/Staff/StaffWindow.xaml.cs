using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.User_Controls;
using GradeVerification.View.Encoder.User_Controls;
using GradeVerification.View.Staff.User_Controls;
using GradeVerification.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GradeVerification.View.Staff
{
    /// <summary>
    /// Interaction logic for StaffWindow.xaml
    /// </summary>
    public partial class StaffWindow : Window
    {
        private Button activeButton; // Track the active button

        private readonly ApplicationDbContext _dbContext;
        public StaffWindow(ApplicationDbContext dbContext, User loggedInUser)
        {
            InitializeComponent();

            _dbContext = dbContext;
            DisplayUserDetails(loggedInUser);
            MainContentControl.Content = new Dashboard();
        }

        private void SetActiveButton(Button clickedButton)
        {
            // Reset the previous active button to normal style
            if (activeButton != null)
            {
                activeButton.Style = (Style)FindResource("menuButton");
            }

            // Set the new active button
            clickedButton.Style = (Style)FindResource("menuButtonActive");
            activeButton = clickedButton;
        }

        private void DisplayUserDetails(User user)
        {
            if (user != null)
            {
                txtUserFullName.Text = user.FullName;
                txtUserRole.Text = user.Role;
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void btn_students(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            var staffStudent = new StaffStudentDashboard();
            staffStudent.DataContext = new StudentDashboardViewModel();
            MainContentControl.Content = staffStudent;
        }

        private void btn_dashboard(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            MainContentControl.Content = new Dashboard();
        }

        private void btn_logout(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to log out?", "Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Create a new login window before closing all others
                MainWindow login = new MainWindow(new ApplicationDbContext());
                this.Close();
                login.Show();
            }
        }
    }
}
