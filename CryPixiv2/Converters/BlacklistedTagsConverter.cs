using System;
using System.Linq;
using Windows.UI.Xaml.Data;
using System.Collections.Generic;

namespace CryPixiv2.Converters
{
    public class BlacklistedTagsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var src = (List<string>)value;
            var text = "";

            foreach (var s in src) text += s + "\r";
            
            return text;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var text = (string)value;

            var lines = text.Split("\r");
            var actuallines = new List<string>();

            foreach (var l in lines)
            {
                if (l.Length == 0) continue;
                if (l.Contains('\r') || l.Contains('\n')) continue;
                if (actuallines.Contains(l.ToLower().Trim())) continue;

                actuallines.Add(l.ToLower().Trim());
            }

            return actuallines;
        }
    }
}
