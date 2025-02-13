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
using System.Windows.Shapes;

namespace GradeVerification.View.Admin.Windows
{
    /// <summary>
    /// Interaction logic for AddSubject.xaml
    /// </summary>
    public partial class AddSubject : Window
    {
        private readonly ApplicationDbContext _dbContext;
        public AddSubject(ApplicationDbContext dbContext)
        {
            InitializeComponent();
            _dbContext = dbContext;
            DataContext = new AddSubjectViewModel();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void btn_Minimize(object sender, RoutedEventArgs e)
        {

        }

        private void btn_Close(object sender, RoutedEventArgs e)
        {

        }
    }
}
