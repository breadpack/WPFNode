using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace WPFNode.Converters
{
    [ValueConversion(typeof(object), typeof(bool))]
    public class IsTypeConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null || !(parameter is Type targetTypeToCheck))
                return false;

            Type valueType = value.GetType();
            
            // 지정된 타입이 인터페이스인 경우
            if (targetTypeToCheck.IsInterface)
            {
                return targetTypeToCheck.IsAssignableFrom(valueType);
            }
            
            // 상속 관계 확인
            return targetTypeToCheck.IsAssignableFrom(valueType);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
} 