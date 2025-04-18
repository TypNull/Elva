using System;
using System.Globalization;
using System.Windows.Data;

namespace Elva.Common.Converters
{
    /// <summary>
    /// Compare two Values with each other and returns if equals
    /// </summary>
    public class WindowConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[1] is System.Windows.Window wnd)
                return wnd.Height == wnd.RestoreBounds.Height && wnd.Width == wnd.RestoreBounds.Width;
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
