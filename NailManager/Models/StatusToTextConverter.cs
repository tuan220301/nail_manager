using System;
using System.Globalization;
using System.Windows.Data;

namespace NailManager.Models
{
    public class StatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int status)
            {
                return status switch
                {
                    1 => "PROCESSING",
                    2 => "COMPLETE",
                    0 => "CANCLE",
                    _ => "UNKNOWN",
                };
            }

            return "UNKNOWN";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}