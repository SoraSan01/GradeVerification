using GradeVerification.View.Admin.Windows;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using GradeVerification.View.Admin.Windows;
using GradeVerification.Data;
using GradeVerification.ViewModel;

namespace GradeVerification.View.Admin.User_Controls
{
    /// <summary>
    /// Interaction logic for UserDashboard.xaml
    /// </summary>
    public partial class UserDashboard : UserControl
    {
        private readonly ApplicationDbContext _dbContext;

        public UserDashboard(ApplicationDbContext dbContext)
        {
            InitializeComponent();
            _dbContext = dbContext;
            DataContext = new UserDashboardViewModel(_dbContext);
        }
    }
}
