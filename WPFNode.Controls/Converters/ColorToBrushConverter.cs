using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WPFNode.Controls;

public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(Color.FromArgb(
                30,  // 알파값을 30으로 고정하여 반투명하게
                color.R,
                color.G,
                color.B));
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 
