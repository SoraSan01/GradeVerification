using GradeVerification.Data;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GradeVerification.View.Admin.User_Controls
{
    /// <summary>
    /// Interaction logic for StudentDashboard.xaml
    /// </summary>
    public partial class StudentDashboard : UserControl
    {
        ApplicationDbContext _dbContext;
        public StudentDashboard(ApplicationDbContext dbContext)
        {
            InitializeComponent();
            _dbContext = dbContext;
            DataContext = new StudentDashboardViewModel();
        }
    }
}
