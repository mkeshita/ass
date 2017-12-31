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
using MaterialDesignThemes.Wpf;
using norsu.ass.Models;

namespace norsu.ass.Server.Views
{
    /// <summary>
    /// Interaction logic for ChangePasswordDialog.xaml
    /// </summary>
    public partial class ChangePasswordDialog : UserControl
    {
        public ChangePasswordDialog(User user)
        {
            InitializeComponent();
            DataContext = user;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (Password.Password != Password2.Password)
            {
                MessageBox.Show("Password did not match.");
                return;
            }
            if (Password.Password.Length == 0)
            {
                MessageBox.Show("Password is required.");
                return;
            }
            
            var user = DataContext as User;
            if (user == null) return;
            if (OldPassword.Password != user.Password)
            {
                MessageBox.Show("Invalid password.");
                return;
            }
            user.Update(nameof(user.Password),Password.Password);
            DialogHost.CloseDialogCommand.Execute(true,this);
        }
    }
}
