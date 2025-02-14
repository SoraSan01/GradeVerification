using GradeVerification.Model;
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
    /// Interaction logic for EditSubject.xaml
    /// </summary>
    public partial class EditSubject : Window
    {
        public EditSubject(Subject subject)
        {
            InitializeComponent();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove(); // Allows window dragging when the border is clicked
            }
        }

        private void btn_Close(object sender, RoutedEventArgs e)
        {
            this.Close(); // Closes the window
        }

        private void btn_Minimize(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized; // Minimizes the window
        }
    }
}
