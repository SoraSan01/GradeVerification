﻿using GradeVerification.ViewModel;
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
    /// Interaction logic for SettingsDashboard.xaml
    /// </summary>
    public partial class SettingsDashboard : UserControl
    {
        public SettingsDashboard()
        {
            InitializeComponent();
            DataContext = new SettingsDashboardViewModel();
        }
    }
}
