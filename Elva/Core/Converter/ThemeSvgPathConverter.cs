using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Elva.Core.Converter
{
    public class ThemeSvgPathConverter : IValueConverter
    {
        public string LightThemePath { get; set; } = "\\Recource\\Icons\\Dark\\";
        public string DarkThemePath { get; set; } = "\\Recource\\Icons\\Dark\\";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            string fileName = value.ToString();

            // Check if we should use dark or light theme
            bool isDarkTheme = IsAppUsingDarkTheme();
            // Return the path to the appropriate themed icon
            if (isDarkTheme)
            {
                return DarkThemePath + fileName;
            }
            else
            {
                return LightThemePath + fileName;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private bool IsAppUsingDarkTheme()
        {
            // Method 1: Check TextPrimary color - if it's light, we're in dark theme
            if (Application.Current.Resources.Contains("TextPrimary"))
            {
                SolidColorBrush? textBrush = Application.Current.Resources["TextPrimary"] as SolidColorBrush;
                if (textBrush != null)
                {
                    // Check if color is light (simplified - you might need a better algorithm)
                    Color color = textBrush.Color;
                    double brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
                    return brightness > 0.5; // If text is light, we're in dark theme
                }
            }

            // Method 2: Check BackgroundPrimary - if it's dark, we're in dark theme
            if (Application.Current.Resources.Contains("BackgroundPrimary"))
            {
                SolidColorBrush? bgBrush = Application.Current.Resources["BackgroundPrimary"] as SolidColorBrush;
                if (bgBrush != null)
                {
                    Color color = bgBrush.Color;
                    double brightness = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
                    return brightness < 0.5; // If background is dark, we're in dark theme
                }
            }

            // Default to light theme if we can't determine
            return false;
        }
    }
}