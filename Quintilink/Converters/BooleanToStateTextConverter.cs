using System;
using System.Globalization;
using System.Windows.Data;

namespace Quintilink.Converters
{
    /// <summary>
    /// Converts boolean values to readable On/Off text for accessibility.
    /// </summary>
    public class BooleanToStateTextConverter : IValueConverter
    {
        public string OnText { get; set; } = "On";
        public string OffText { get; set; } = "Off";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolean && boolean ? OnText : OffText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
