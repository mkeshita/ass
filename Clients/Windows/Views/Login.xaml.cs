using System.Windows;
using System.Windows.Controls;
using norsu.ass.Server.ViewModels;

namespace norsu.ass.Server.Views
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : UserControl
    {
        public Login()
        {
            InitializeComponent();
            Messenger.Default.AddListener(Messages.Logout, () =>
            {
                PasswordBox.Password = "";
            });
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ((LoginViewModel) DataContext).Password = ((PasswordBox) sender).Password;
        }
    }
}
