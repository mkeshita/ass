using System.ComponentModel;
using System.Windows;

namespace norsu.ass.Server.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView 
    {
        public MainView()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Network.Server.Instance.Stop();
            base.OnClosing(e);
        }
    }
}
