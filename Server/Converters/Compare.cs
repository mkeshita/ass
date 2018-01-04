using System;
using System.Windows.Data;

namespace norsu.ass.Server.Converters
{
    public enum Comparison
    {
        Equal,
        LessThan,
        GreaterThan
    }
    class Compare : ConverterBase
    {
        public Comparison Comparison { get; set; }

        public Compare()
        {
            Comparison = Comparison.Equal;
        }

        public Compare(Comparison comparison)
        {
            Comparison = comparison;
        }

        protected override object Convert(object value, Type targetType, object parameter)
        {
            var lVal = (double)value;
            var lParam = double.Parse(parameter.ToString());
            switch (Comparison)
            {
                case Comparison.Equal:
                    return lVal == lParam;
                case Comparison.LessThan:
                    return lVal < lParam;
                case Comparison.GreaterThan:
                    return lVal > lParam;
                default:
                    return Binding.DoNothing;
            }
        }
    }
}
