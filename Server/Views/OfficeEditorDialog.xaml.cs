using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using norsu.ass.Models;
using System.Drawing;
using Microsoft.Win32;
using norsu.ass.Server.Properties;

namespace norsu.ass.Server.Views
{
    /// <summary>
    /// Interaction logic for OfficeEditorDialog.xaml
    /// </summary>
    public partial class OfficeEditorDialog : UserControl
    {
      
        public OfficeEditorDialog()
        {
            InitializeComponent();
        }

        private void UIElement_OnPreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
                var file = files?.FirstOrDefault();
                if (!ImageProcessor.IsAccepted(file))
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                    return;
                }
                e.Effects = DragDropEffects.All;
            }
            e.Effects = DragDropEffects.None;
        }

        private void UIElement_OnPreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files =(string[]) e.Data.GetData(DataFormats.FileDrop);
                var file = files?.FirstOrDefault();
                if (file == null) return;
                if(!ImageProcessor.IsAccepted(file)) return;
                
                using(var img = System.Drawing.Image.FromFile(file))
                {
                    using (var bmp = ImageProcessor.Resize(img, 128))
                    {
                        using (var bin = new MemoryStream())
                        {
                            bmp.Save(bin, ImageFormat.Jpeg);
                            ((Office) DataContext).Picture = bin.ToArray();
                        }
                    }  
                }
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Title = "Select Picture";
            dlg.Multiselect = false;
            dlg.Filter = @"All Images|*.BMP;*.JPG;*.JPEG;*.GIF;*.PNG|
                               BMP Files|*.BMP;*.DIB;*.RLE|
                               JPEG Files|*.JPG;*.JPEG;*.JPE;*.JFIF|
                               GIF Files|*.GIF|
                               PNG Files|*.PNG";
            
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            if (!dlg.ShowDialog(App.Current.MainWindow) ?? false) return;

            using (var img = System.Drawing.Image.FromFile(dlg.FileName))
            {
                using (var bmp = ImageProcessor.Resize(img, 128))
                {
                    using (var bin = new MemoryStream())
                    {
                        bmp.Save(bin, ImageFormat.Jpeg);
                        ((Office) DataContext).Picture = bin.ToArray();
                    }
                }
            }
        }
        
    }
}
