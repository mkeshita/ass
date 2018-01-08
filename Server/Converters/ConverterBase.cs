using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace norsu.ass.Server.Converters
{
    internal abstract class ConverterBase : MarkupExtension, IValueConverter
    {
        protected ConverterBase()
        {
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter);
        }

        protected abstract object Convert(object value, Type targetType, object parameter);

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}