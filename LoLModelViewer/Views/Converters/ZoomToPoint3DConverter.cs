using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Media3D;

namespace LoLModelViewer.Views.Converters
{
    public class ZoomToPoint3DConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double zoom)
            {
                return new Point3D(0, 300, zoom);
            }
            return new Point3D(0, 300, 400); // Default value
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
