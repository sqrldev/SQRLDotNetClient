using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace SQRLCommonUI.AvaloniaExtensions
{
    class StringUppercaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString().ToUpper();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString().ToLower();
        }
    }
}
