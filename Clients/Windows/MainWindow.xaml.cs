using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using norsu.ass.Server.ViewModels;
using norsu.ass.Server.Views;

namespace Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
        }

        protected override void OnInitialized(EventArgs e)
        {
           base.OnInitialized(e);
           MainViewModel.Instance.Download();
        }

        private void MinimizeClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MaximizeClicked(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            DragMove();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ContextMenu.PlacementTarget = (UIElement) sender;
            ContextMenu.Placement = PlacementMode.Right;
            ContextMenu.IsOpen = true;
        }
    }
}
