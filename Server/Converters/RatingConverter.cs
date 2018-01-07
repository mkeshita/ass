using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using norsu.ass.Models;

namespace norsu.ass.Server.Converters
{
    class RatingConverter : ConverterBase
    {
        private int Star { get; set; } = 0;
        private double Width { get; set; }
        
        public RatingConverter(int star,double width)
        {
            Star = star;
            Width = width;
        }
        
        protected override object Convert(object value, Type targetType, object parameter)
        {
            if (!(value is Office office)) return Binding.DoNothing;
            
            var total = Rating.Cache.Count(d => d.OfficeId == office.Id);
            var stars = Rating.Cache.Count(d => d.Value == Star && d.OfficeId == office.Id);
            return ((stars+0f) / total) * Width;
        }
    }
}
