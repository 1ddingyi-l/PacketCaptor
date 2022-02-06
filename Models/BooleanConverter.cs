using System;
using System.Globalization;
using System.Windows.Data;

namespace DotNetPacketCaptor.Models
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) 
                throw new NullReferenceException();
            // Get that value
            var v = (bool) value;
            return !v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
