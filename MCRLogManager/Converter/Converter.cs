using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MCRLogManager.Converter
{

    class VisibilityToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Visibility b)) { return DependencyProperty.UnsetValue; }
            return (b == Visibility.Visible);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool b)) { return DependencyProperty.UnsetValue; }
            if (b)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

    }

    class HalfDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double b)) { return DependencyProperty.UnsetValue; }
            return b / 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double b)) { return DependencyProperty.UnsetValue; }
            return b * 2;
        }

    }
}
