using System;
using System.Globalization;
using System.Windows.Data;

namespace Elva.Core.Converter
{
    /// <summary>
    /// Converter that checks if a progress value is within a download-in-progress range (1-99)
    /// </summary>
    internal class ProgressRangeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int progress)
            {
                // Return true if progress is between 1 and 99
                return progress > 0 && progress < 100;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}