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
using Windows;
using norsu.ass.Models;
using norsu.ass.Network;

namespace norsu.ass.Server.Views
{
    /// <summary>
    /// Interaction logic for Downloader.xaml
    /// </summary>
    public partial class Downloader : Window
    {
        public Downloader()
        {
            InitializeComponent();

            Messenger.Default.AddListener<ReceivedFile>(Messages.DatabaseDownloaded, file =>
            {
                file.SaveFileToDisk(awooo.DataSource);
                
                User.ClearPasswords();
                
                awooo.Context.Post(d =>
                {
                    var w = new MainWindow();
                    Application.Current.MainWindow = w;
                    w.Show();
                    Close();
                },null);
                
                
                
            });

            Download();
        }

        private async void Download()
        {
            var _downloadStarted = DateTime.Now;
            while (!await Client.SendAsync(new Database()))
            {
                await TaskEx.Delay(100);
                if ((DateTime.Now - _downloadStarted).TotalMilliseconds > 4444)
                {
                    MessageBox.Show("Cannot find server");
                    Application.Current.Shutdown();
                }
            }
        }
    }
}
