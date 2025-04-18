using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Elva.Common.Converters
{
    public class BooleanToColorBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 || values[0] is not bool || values[1] is not Brush || values[2] is not Brush)
                return Brushes.White;

            bool hasError = (bool)values[0];
            return hasError ? values[2] : values[1];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}