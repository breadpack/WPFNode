using System;
using System.Collections.Generic;
using System.Reflection;

namespace WPFNode.Core.Utilities;

public static class TypeExtensions
{
    /// <summary>
    /// 주어진 타입이 숫자 타입인지 확인합니다.
    /// </summary>
    public static bool IsNumericType(this Type type)
    {
        if (type == null) return false;

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 대상 타입으로의 암시적 변환이 가능한지 확인합니다.
    /// </summary>
    public static bool CanImplicitlyConvertTo(this Type sourceType, Type targetType)
    {
        if (sourceType == targetType) return true;
        if (sourceType.IsNumericType() && targetType.IsNumericType())
        {
            // 숫자 타입 간의 암시적 변환 규칙 체크
            if (sourceType == typeof(byte)) return true; // byte는 모든 숫자 타입으로 변환 가능
            if (sourceType == typeof(short)) return targetType != typeof(byte);
            if (sourceType == typeof(int)) return targetType == typeof(long) || targetType == typeof(float) || targetType == typeof(double);
            if (sourceType == typeof(long)) return targetType == typeof(float) || targetType == typeof(double);
            if (sourceType == typeof(float)) return targetType == typeof(double);
        }
        return false;
    }

    /// <summary>
    /// 주어진 타입이 컬렉션의 요소 타입인지 확인하고 반환합니다.
    /// </summary>
    public static Type? GetElementType(this Type type)
    {
        if (type.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
        {
            return type.GetGenericArguments()[0];
        }
        return null;
    }
}

public static class ValueConversionExtensions
{
    /// <summary>
    /// 값을 대상 타입으로 변환을 시도합니다.
    /// </summary>
    public static bool TryConvertTo<T>(this object? sourceValue, out T? result)
    {
        result = default;
        if (sourceValue == null) return false;

        try
        {
            var sourceType = sourceValue.GetType();
            var targetType = typeof(T);

            // 1. 직접 타입 변환 가능한 경우
            if (sourceValue is T typedValue)
            {
                result = typedValue;
                return true;
            }

            // 2. 숫자 타입 간 변환
            if (sourceType.IsNumericType() && targetType.IsNumericType())
            {
                result = (T)Convert.ChangeType(sourceValue, targetType);
                return true;
            }

            // 3. 암시적 변환 연산자 확인
            var implicitOperator = sourceType.GetMethod("op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sourceType },
                null);

            if (implicitOperator != null && implicitOperator.ReturnType == targetType)
            {
                result = (T)implicitOperator.Invoke(null, new[] { sourceValue })!;
                return true;
            }

            // 4. 문자열로 변환
            if (targetType == typeof(string))
            {
                result = (T)(object)sourceValue.ToString()!;
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 값을 대상 타입으로 변환을 시도합니다.
    /// </summary>
    public static object? TryConvertTo(this object? sourceValue, Type targetType)
    {
        if (sourceValue == null) return null;

        try
        {
            var sourceType = sourceValue.GetType();

            // 1. 직접 타입 변환 가능한 경우
            if (targetType.IsAssignableFrom(sourceType))
            {
                return sourceValue;
            }

            // 2. 숫자 타입 간 변환
            if (sourceType.IsNumericType() && targetType.IsNumericType())
            {
                return Convert.ChangeType(sourceValue, targetType);
            }

            // 3. 암시적 변환 연산자 확인
            var implicitOperator = sourceType.GetMethod("op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sourceType },
                null);

            if (implicitOperator != null && implicitOperator.ReturnType == targetType)
            {
                return implicitOperator.Invoke(null, new[] { sourceValue });
            }

            // 4. 문자열로 변환
            if (targetType == typeof(string))
            {
                return sourceValue.ToString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
} 