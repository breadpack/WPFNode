using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPFNode.Converters
{
    public class BoldIfCategoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCategory && isCategory)
            {
                return FontWeights.Bold;
            }
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 