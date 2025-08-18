using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LoLModelViewer.Views.Converters
{
    public class BrightnessToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double brightness)
            {
                byte component = (byte)(brightness * 255);
                return Color.FromRgb(component, component, component);
            }
            return Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
