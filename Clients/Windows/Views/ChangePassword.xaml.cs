using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using norsu.ass.Server.ViewModels;

namespace norsu.ass.Server.Views
{
    /// <summary>
    /// Interaction logic for ChangePassword.xaml
    /// </summary>
    public partial class ChangePassword : UserControl
    {
        public ChangePassword()
        {
            InitializeComponent();
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ((MainViewModel) DataContext).ChangePassword.CurrentPassword = ((PasswordBox) sender).Password;
        }

        private void NewPassword_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ((MainViewModel) DataContext).ChangePassword.NewPassword = ((PasswordBox) sender).Password;
        }

        private void NewPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ((MainViewModel) DataContext).ChangePassword.NewPassword2 = ((PasswordBox) sender).Password;
        }
    }
}
