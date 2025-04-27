using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Elva.Common.Converters
{
    public class ThemePathConverter : IValueConverter
    {
        public string LightThemePath { get; set; } = "Light\\";
        public string DarkThemePath { get; set; } = "Dark\\";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null!;

            string fileName = value.ToString() ?? "";
            bool isDarkTheme = IsAppUsingDarkTheme();
            if (isDarkTheme)
                return DarkThemePath + fileName;
            else
                return LightThemePath + fileName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static bool IsAppUsingDarkTheme()
        {
            if (Application.Current.Resources.Contains("TextPrimary"))
            {
                if (Application.Current.Resources["TextPrimary"] is SolidColorBrush textBrush)
                {
                    Color color = textBrush.Color;
                    double brightness = ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255;
                    return brightness > 0.5;
                }
            }

            if (Application.Current.Resources.Contains("BackgroundPrimary"))
            {
                if (Application.Current.Resources["BackgroundPrimary"] is SolidColorBrush bgBrush)
                {
                    Color color = bgBrush.Color;
                    double brightness = ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255;
                    return brightness < 0.5;
                }
            }
            return false;
        }
    }
}