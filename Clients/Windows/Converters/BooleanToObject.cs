using System;

namespace norsu.ass.Server.Converters
{
    class BooleanToObject:ConverterBase
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }
        
        public BooleanToObject(object trueValue, object falseValue=null)
        {
            TrueValue = trueValue;
            FalseValue = falseValue;
        }

        protected override object Convert(object value, Type targetType, object parameter)
        {
            if (value == null) return FalseValue;
            if((value as bool?) ?? false) return TrueValue;
            return FalseValue;

        }
    }
}
