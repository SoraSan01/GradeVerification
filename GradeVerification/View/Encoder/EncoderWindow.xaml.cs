using GradeVerification.Data;
using GradeVerification.Model;
using GradeVerification.View.Admin.User_Controls;
using GradeVerification.View.Encoder.User_Controls;
using GradeVerification.ViewModel;
using Microsoft.EntityFrameworkCore;
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

namespace GradeVerification.View.Encoder
{
    /// <summary>
    /// Interaction logic for EncoderWindow.xaml
    /// </summary>
    public partial class EncoderWindow : Window
    {
        private Button activeButton; // Track the active button

        private readonly ApplicationDbContext _dbContext;
        public EncoderWindow(ApplicationDbContext dbContext, User loggedInUser)
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
            var encoderStudent = new EncoderStudentDashboard();
            encoderStudent.DataContext = new StudentDashboardViewModel();
            MainContentControl.Content = encoderStudent;
        }

        private void btn_dashboard(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            MainContentControl.Content = new Dashboard();
        }

        private void btn_grades(object sender, RoutedEventArgs e)
        {
            SetActiveButton((Button)sender);
            var encoderGrades = new EncoderGradeDashboard();
            encoderGrades.DataContext = new GradeDashboardViewModel();
            MainContentControl.Content = encoderGrades;
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

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
