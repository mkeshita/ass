using System;
using System.Windows;

namespace norsu.ass.Server.Converters {
    class BooleanToVisibility : ConverterBase
    {
        private Visibility trueVisibility = Visibility.Visible;
        private readonly Visibility falseVisibility = Visibility.Collapsed;

        public BooleanToVisibility(Visibility whenTrue,Visibility whenFalse)
        {
            trueVisibility = whenTrue;
            falseVisibility = whenFalse;
        }
        public BooleanToVisibility() { }

        protected override object Convert(object value, Type targetType, object parameter)
        {
            if (parameter != null) trueVisibility = (Visibility) parameter;
            if(value==null) return falseVisibility;
            if(value is bool b)
                return b ? trueVisibility : falseVisibility;
            return trueVisibility;
        }
    }
}
