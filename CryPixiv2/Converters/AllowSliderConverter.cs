using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace CryPixiv2.Converters
{
    public class AllowSliderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var val = (double)value;
            switch (val)
            {
                case 0: return "Safe";
                case 1: return "Questionable";
                case 2: return "NSFW";
                default: return "Unknown";
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language) => null;
    }
}
