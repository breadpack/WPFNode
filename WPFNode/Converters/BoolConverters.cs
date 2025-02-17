using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPFNode.Converters;

public class BoolToColumnConverter : IValueConverter
{
    public int TrueValue { get; set; }
    public int FalseValue { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToAlignmentConverter : IValueConverter
{
    public HorizontalAlignment TrueValue { get; set; }
    public HorizontalAlignment FalseValue { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToTextAlignmentConverter : IValueConverter
{
    public TextAlignment TrueValue { get; set; }
    public TextAlignment FalseValue { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToMarginConverter : IValueConverter
{
    public Thickness TrueValue { get; set; }
    public Thickness FalseValue { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 