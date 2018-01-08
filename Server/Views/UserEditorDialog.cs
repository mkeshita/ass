using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Devcorner.NIdenticon;
using Devcorner.NIdenticon.BrushGenerators;
using MaterialDesignThemes.Wpf;
using norsu.ass.Models;
using Color = System.Drawing.Color;

namespace norsu.ass.Server.Views
{
    /// <summary>
    /// Interaction logic for NewUserEditor.xaml
    /// </summary>
    public partial class UserEditorDialog : UserControl
    {
        public UserEditorDialog(string title, User dataContext, Visibility levelSelectorVisibility = Visibility.Collapsed)
        {
            InitializeComponent();
            Title.Text = title;
            DataContext = dataContext;
            AccessListBox.Visibility = levelSelectorVisibility;
        }

        private void UIElement_OnPreviewDrop(object sender, DragEventArgs e)
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

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var rnd = new Random();
            var gen = new IdenticonGenerator()
                .WithBlocks(7, 7)
                .WithSize(128, 128)
                .WithBlockGenerators(IdenticonGenerator.ExtendedBlockGeneratorsConfig)
                .WithBackgroundColor(Color.White)
                .WithBrushGenerator(new StaticColorBrushGenerator(Color.FromArgb(255,rnd.Next(0,255),rnd.Next(0,255),rnd.Next(0,255))));

            using (var pic = gen.Create("awooo" + DateTime.Now.Ticks))
            {
                using (var stream = new MemoryStream())
                {
                    var usr = (User) DataContext;
                    pic.Save(stream, ImageFormat.Jpeg);
                    usr.Picture = stream.ToArray();
                }
            }

        }

        private void UIElement_OnPreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
                var file = files?.FirstOrDefault();
                if (file == null)
                    return;
                if (!ImageProcessor.IsAccepted(file))
                    return;

                using (var img = System.Drawing.Image.FromFile(file))
                {
                    using (var bmp = ImageProcessor.Resize(img, 128))
                    {
                        using (var bin = new MemoryStream())
                        {
                            bmp.Save(bin, ImageFormat.Jpeg);
                            ((User) DataContext).Picture = bin.ToArray();
                        }
                    }
                }
            }
        }

        private void PasswordBox2_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password != PasswordBox2.Password)
            {
                Button.IsEnabled = false;
                return;
            }

            ((User) DataContext).Password = PasswordBox.Password;
            Button.IsEnabled = true;
        }
        
        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var usr  = (User) DataContext;
            if (!usr.CanSave()) return;
            if (usr.Id == 0 && User.Cache.Any(x => x.Username.ToLower() == usr.Username.ToLower()))
            {
                //TODO dialog
                MessageBox.Show("Username is already taken.");
                return;
            }
            DialogHost.CloseDialogCommand.Execute(true,this);
        }
    }
}
