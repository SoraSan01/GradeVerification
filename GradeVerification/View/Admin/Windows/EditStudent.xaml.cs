﻿using System;
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
    /// Interaction logic for EditStudent.xaml
    /// </summary>
    public partial class EditStudent : Window
    {
        public EditStudent()
        {
            InitializeComponent();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void btn_Minimize(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized; // Minimizes the window
        }

        private void btn_Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
