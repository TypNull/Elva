using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Elva.Common.Converters
{
    internal class DictionaryBoolConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null || values.Length != 2)
                return false;
            string? key = values[0] as string;


            if (string.IsNullOrEmpty(key) || values[1] is not Dictionary<string, bool?> directory || !directory.TryGetValue(key, out bool? value))
                return false;
            return value;
        }

        public object[]? ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
