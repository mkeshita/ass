using System;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace norsu.ass.Server.Converters
{
    class BytesToImage : ConverterBase
    {
        protected override object Convert(object value, Type targetType, object parameter)
        {
            if (value == null) return Binding.DoNothing;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;

            using (var mem = new MemoryStream((byte[])value))
            {
                bmp.StreamSource = mem;
                bmp.EndInit();
                bmp.Freeze();
            }

            return bmp;
        }
    }
}
