using GradeVerification.View.Admin.User_Controls;
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
using GradeVerification.Data;
using GradeVerification.Model;

namespace GradeVerification.View.Admin
{
    /// <summary>
    /// Interaction logic for AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        private Button activeButton; // Track the active button

        private readonly ApplicationDbContext _dbContext;
        public AdminWindow(ApplicationDbContext dbContext, User loggedInUser)
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
            if (e.ChangedButton == MouseButton.Left) {
                this.DragMove();
            }
        }

        private void btn_students(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            MainContentControl.Content = new StudentDashboard(_dbContext);
        }

        private void btn_dashboard(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            MainContentControl.Content = new Dashboard();
        }

        private void btn_grades(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            MainContentControl.Content = new GradeDashboard();
        }

        private void btn_subjects(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            var subjectDashboard = new SubjectsDashboard(_dbContext);
            MainContentControl.Content = subjectDashboard;
        }

        private void btn_programs(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            var programDashbaord = new ProgramDashboard(_dbContext);
            MainContentControl.Content = programDashbaord;
        }

        private void btn_users(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            var studentDashboard = new UserDashboard(_dbContext);
            MainContentControl.Content = studentDashboard;
        }

        private void btn_settings(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            var settingDashboard = new SettingsDashboard();
            MainContentControl.Content = settingDashboard;
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

        private void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}
