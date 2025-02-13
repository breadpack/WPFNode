using System;
using System.Globalization;
using System.Windows.Data;

namespace WPFNode.Controls.Converters
{
    public class CenterOffsetConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 || 
                values[0] is not double position ||
                values[1] is not double canvasSize)
                return 0;

            // Canvas 크기의 절반을 더해서 중심을 (0,0)으로 만듦
            return position + (canvasSize / 2);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 