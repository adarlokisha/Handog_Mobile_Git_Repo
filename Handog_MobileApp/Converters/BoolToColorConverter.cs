using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Handog_MobileApp.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public Color SelectedColor { get; set; } = Colors.LightBlue;
        public Color DefaultColor { get; set; } = Colors.Transparent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
                return isSelected ? SelectedColor : DefaultColor;
            return DefaultColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
