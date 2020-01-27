using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static SQRLDotNetClientUI.ViewModels.AuthenticationViewModel;

namespace SQRLDotNetClientUI.AvaloniaExtensions
{
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.Equals(Enum.Parse(typeof(LoginAction),(string)parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.Equals(true)==true  ? Enum.Parse(typeof(LoginAction), (string)parameter) : LoginAction.Login;
        }
    }
}
