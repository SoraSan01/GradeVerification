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

namespace GradeVerification.View.Admin
{
    /// <summary>
    /// Interaction logic for AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        private readonly ApplicationDbContext _dbContext;
        public AdminWindow(ApplicationDbContext dbContext)
        {
            InitializeComponent();
            _dbContext = dbContext;

            MainContentControl.Content = new Dashboard();
        }


        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) {
                this.DragMove();
            }
        }

        private void btn_students(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new StudentDashboard(_dbContext);
        }

        private void btn_dashboard(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new Dashboard();
        }

        private void btn_grades(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new GradeDashboard();
        }

        private void btn_subjects(object sender, RoutedEventArgs e)
        {
            var subjectDashboard = new SubjectsDashboard(_dbContext);
            MainContentControl.Content = subjectDashboard;
        }

        private void btn_programs(object sender, RoutedEventArgs e)
        {
            var programDashbaord = new ProgramDashboard(_dbContext);
            MainContentControl.Content = programDashbaord;
        }

        private void btn_users(object sender, RoutedEventArgs e)
        {

            var studentDashboard = new UserDashboard(_dbContext);
            MainContentControl.Content = studentDashboard;
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
