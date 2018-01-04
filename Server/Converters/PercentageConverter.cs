using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace norsu.ass.Server.Converters
{
    class PercentageConverter : ConverterBase
    {
        
        protected override object Convert(object value, Type targetType, object parameter)
        {
            var percent = (double)value;
            var maxValue = (double) parameter;
            return maxValue * percent;
        }
    }
}
