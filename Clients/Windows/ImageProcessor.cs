using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using Windows;
using Devcorner.NIdenticon;
using Devcorner.NIdenticon.BrushGenerators;
using Microsoft.Win32;
using norsu.ass.Models;

namespace norsu.ass.Server
{
    static class ImageProcessor
    {
        public const string ACCEPTED_EXTENSIONS = @".BMP.JPG.JPEG.GIF.PNG.BMP.DIB.RLE.JPE.JFIF";

        public static byte[] GetPicture(int size=128)
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

            if (! dlg.ShowDialog(Application.Current.MainWindow)??false) return null;

            using (var img = System.Drawing.Image.FromFile(dlg.FileName))
            {
                using (var bmp = Resize(img, size))
                {
                    using (var bin = new MemoryStream())
                    {
                        bmp.Save(bin, ImageFormat.Jpeg);
                        return bin.ToArray();
                    }
                }
            }
        }

        public static bool IsAccepted(string file)
        {
            if (file == null) return false;
            var ext = System.IO.Path.GetExtension(file)?.ToUpper();
            return File.Exists(file) && (ACCEPTED_EXTENSIONS.Contains(ext));
            
        }

        public static byte[] Generate()
        {
            var rnd = new Random();
            var color = Color.FromArgb(255, rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255));
            var gen = new IdenticonGenerator()
                .WithBlocks(7, 7)
                .WithSize(128, 128)
                .WithBlockGenerators(IdenticonGenerator.ExtendedBlockGeneratorsConfig)
                .WithBackgroundColor(Color.White)
                .WithBrushGenerator(new StaticColorBrushGenerator(color));
            
            using (var pic = gen.Create("awooo" + DateTime.Now.Ticks))
            {
                using (var stream = new MemoryStream())
                {
                    pic.Save(stream, ImageFormat.Jpeg);
                    return stream.ToArray();
                }
            }
            
        }

        public static Image Resize(Image imgPhoto, int size)
        {
            return Resize(imgPhoto, size, Color.White);
        }

        public static Image Resize(Image imgPhoto, int size, Color background)
        {
            var sourceWidth = imgPhoto.Width;
            var sourceHeight = imgPhoto.Height;
            var sourceX = 0;
            var sourceY = 0;
            var destX = 0;
            var destY = 0;
            var nPercent = 0.0f;

            if (sourceWidth > sourceHeight)
                nPercent = (size / (float) sourceWidth);

            else
                nPercent = (size / (float) sourceHeight);



            var destWidth = (int) (sourceWidth * nPercent);
            var destHeight = (int) (sourceHeight * nPercent);

            var bmPhoto = new Bitmap(destWidth, destHeight, PixelFormat.Format32bppArgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

            var grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;
            grPhoto.Clear(background);
            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }
    }
}
