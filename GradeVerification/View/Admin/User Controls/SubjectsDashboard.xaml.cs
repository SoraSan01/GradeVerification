using GradeVerification.Data;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GradeVerification.View.Admin.User_Controls
{
    /// <summary>
    /// Interaction logic for SubjectsDashboard.xaml
    /// </summary>
    public partial class SubjectsDashboard : UserControl
    {
        private readonly ApplicationDbContext _dbContext;
        public SubjectsDashboard(ApplicationDbContext dbContext)
        {
            InitializeComponent();
            _dbContext = dbContext;
            DataContext = new SubjectDashboardViewModel();
        }
    }
}
