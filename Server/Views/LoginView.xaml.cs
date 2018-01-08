using System.Windows;
using System.Windows.Controls;
using norsu.ass.Server.ViewModels;

namespace norsu.ass.Server.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ((MainViewModel) DataContext).Password = ((PasswordBox) sender).Password;
            
        }
        
        
    }
}
