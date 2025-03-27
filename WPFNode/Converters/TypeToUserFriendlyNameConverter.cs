using System;
using System.Globalization;
using System.Windows.Data;
using WPFNode.Extensions;

namespace WPFNode.Converters;

public class TypeToUserFriendlyNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Type type)
        {
            return type.GetUserFriendlyName();
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 