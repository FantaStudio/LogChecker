using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LogAnalyzer.Converters
{
    public class ProgressVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!int.TryParse(value.ToString(), out int progress) || progress < 1 || progress >= 100)
                return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value;
        }
    }
}
