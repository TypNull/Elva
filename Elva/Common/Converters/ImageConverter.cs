using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Elva.Common.Converters
{
    internal class ImageConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine("Converter");
            Debug.WriteLine(value == null);
            Debug.WriteLine(value is string);
            try
            {
                if (value is string path && File.Exists(path))
                {
                    Debug.WriteLine(path);
                    Image png = Image.FromFile(path);
                    png.Save(path, ImageFormat.Jpeg);
                    png.Dispose();
                    return new BitmapImage() { UriSource = new(path) };
                }
            }
            catch (Exception)
            {
            }
            return "pack://application:,,,/Resources/Images/Comic/no_image.jpg";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }


    }
}


