using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPFNode.Converters
{
    /// <summary>
    /// 비어있는 문자열 또는 null인 경우 Visible을 반환하는 컨버터
    /// </summary>
    public class NullToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Visible;
                
            if (value is string s && string.IsNullOrWhiteSpace(s))
                return Visibility.Visible;
                
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
