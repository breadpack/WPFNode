using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WPFNode.Converters;

/// <summary>
/// 타입에 따라 색상을 반환하는 변환기입니다.
/// </summary>
public class TypeToColorConverter : IValueConverter
{
    // 타입별 색상 매핑
    private static readonly Dictionary<Type, Color> TypeColors = new Dictionary<Type, Color>
    {
        { typeof(int), Colors.DodgerBlue },
        { typeof(float), Colors.DeepSkyBlue },
        { typeof(double), Colors.RoyalBlue },
        { typeof(decimal), Colors.SteelBlue },
        { typeof(string), Colors.ForestGreen },
        { typeof(bool), Colors.Crimson },
        { typeof(DateTime), Colors.Purple },
        { typeof(TimeSpan), Colors.DarkViolet },
        { typeof(Guid), Colors.DarkOrange },
        { typeof(byte), Colors.SlateBlue },
        { typeof(char), Colors.Olive },
        { typeof(object), Colors.Gray }
    };

    // 네임스페이스별 색상 매핑
    private static readonly Dictionary<string, Color> NamespaceColors = new Dictionary<string, Color>
    {
        { "System.Collections", Colors.Chocolate },
        { "System.Collections.Generic", Colors.Chocolate },
        { "System.Linq", Colors.DarkGoldenrod },
        { "System.IO", Colors.DarkCyan },
        { "System.Text", Colors.DarkMagenta },
        { "System.Windows", Colors.DarkSlateBlue },
        { "WPFNode", Colors.DarkOliveGreen }
    };

    // 기본 색상
    private static readonly Color DefaultColor = Colors.DimGray;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value == null)
                return new SolidColorBrush(DefaultColor);

            if (value is not Type type)
                return new SolidColorBrush(DefaultColor);

            // 배열 타입 처리
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (elementType == null)
                    return new SolidColorBrush(DefaultColor);
                
                var color = GetColorForType(elementType);
                return new SolidColorBrush(color);
            }

            // 제네릭 컬렉션 타입 처리
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var genericArgs = type.GetGenericArguments();
                if (genericArgs == null || genericArgs.Length == 0)
                    return new SolidColorBrush(DefaultColor);
                
                var elementType = genericArgs[0];
                if (elementType == null)
                    return new SolidColorBrush(DefaultColor);
                
                var color = GetColorForType(elementType);
                return new SolidColorBrush(color);
            }

            // 일반 타입 처리
            var typeColor = GetColorForType(type);
            return new SolidColorBrush(typeColor);
        }
        catch (Exception)
        {
            // 예외 발생 시 기본 색상 반환
            return new SolidColorBrush(DefaultColor);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static Color GetColorForType(Type type)
    {
        if (type == null)
            return DefaultColor;
            
        // 기본 타입 색상 확인
        if (TypeColors.TryGetValue(type, out var color))
            return color;

        // 네임스페이스 기반 색상 확인
        var ns = type.Namespace;
        if (!string.IsNullOrEmpty(ns))
        {
            foreach (var kvp in NamespaceColors)
            {
                if (ns.StartsWith(kvp.Key))
                    return kvp.Value;
            }
        }

        // 기본 색상 반환
        return DefaultColor;
    }
} 