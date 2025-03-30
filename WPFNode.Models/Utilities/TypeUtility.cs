using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace WPFNode.Utilities;

public static class TypeExtensions
{
    /// <summary>
    /// 소스 타입이 대상 타입으로 변환 가능한지 확인합니다.
    /// InputPort와 NodeProperty에서 공통으로 사용되는 CanAcceptType의 기반 메서드입니다.
    /// </summary>
    public static bool CanConvertTo(this Type sourceType, Type targetType)
    {
        if (sourceType == null) return false;
        
        // 1. 대상 타입이 string이면 모든 타입 허용 (ToString 메서드를 통해 변환 가능)
        if (targetType == typeof(string))
            return true;
            
        // 2. 문자열에서 Parse/TryParse 메서드를 통한 변환이 가능한지 확인
        if (sourceType == typeof(string) && targetType.HasParseMethod())
            return true;
            
        // 3. TypeConverter를 통한 변환이 가능한지 확인
        var targetConverter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
        if (targetConverter.CanConvertFrom(sourceType))
            return true;
            
        var sourceConverter = System.ComponentModel.TypeDescriptor.GetConverter(sourceType);
        if (sourceConverter.CanConvertTo(targetType))
            return true;
            
        // 4. 소스 타입에서 정의된 암시적 변환 연산자 확인
        var sourceImplicitOp = sourceType.GetMethod("op_Implicit", 
            BindingFlags.Public | BindingFlags.Static,
            null, 
            new[] { sourceType }, 
            null);
        if (sourceImplicitOp != null && sourceImplicitOp.ReturnType == targetType)
            return true;
        
        // 5. 대상 타입에서 정의된 암시적 변환 연산자 확인
        var targetImplicitOp = targetType.GetMethod("op_Implicit", 
            BindingFlags.Public | BindingFlags.Static,
            null, 
            new[] { sourceType }, 
            null);
        if (targetImplicitOp != null && targetImplicitOp.ReturnType == targetType)
            return true;
        
        // 6. 소스 타입에서 정의된 명시적 변환 연산자 확인
        var sourceExplicitOp = sourceType.GetMethod("op_Explicit", 
            BindingFlags.Public | BindingFlags.Static,
            null, 
            new[] { sourceType }, 
            null);
        if (sourceExplicitOp != null && sourceExplicitOp.ReturnType == targetType)
            return true;
        
        // 7. 대상 타입에서 정의된 명시적 변환 연산자 확인
        var targetExplicitOp = targetType.GetMethod("op_Explicit", 
            BindingFlags.Public | BindingFlags.Static,
            null, 
            new[] { sourceType }, 
            null);
        if (targetExplicitOp != null && targetExplicitOp.ReturnType == targetType)
            return true;
        
        // 8. 대상 타입이 소스 타입을 매개변수로 받는 생성자를 가지고 있는지 확인
        var constructor = targetType.GetConstructor(new[] { sourceType });
        if (constructor != null)
            return true;

        // 9. 타입 호환성 검사 (암시적 변환)
        return sourceType.CanImplicitlyConvertTo(targetType);
    }
    
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

        // 1. 상속 관계 확인
        if (targetType.IsAssignableFrom(sourceType)) return true;

        // 2. Nullable 타입 처리
        if (Nullable.GetUnderlyingType(targetType) != null)
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            return sourceType.CanImplicitlyConvertTo(underlyingType!);
        }

        // 3. 숫자 타입 간의 암시적 변환 규칙 체크
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
        // 1. null 체크
        if (type == null) return null;
        
        // 2. 배열 타입 처리
        if (type.IsArray)
        {
            return type.GetElementType();
        }
        
        // 3. 타입이 IEnumerable를 구현하는지 확인
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            // 제네릭 타입인 경우
            if (type.IsGenericType)
            {
                // 첫 번째 제네릭 인자를 요소 타입으로 간주
                return type.GetGenericArguments()[0];
            }
            
            // 타입의 인터페이스 중에서 IEnumerable<T>를 찾기
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && 
                    iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return iface.GetGenericArguments()[0];
                }
            }
        }
        
        return null;
    }
}

public static class ValueConversionExtensions
{
    /// <summary>
    /// 주어진 타입이 Parse/TryParse 메서드를 가지고 있는지 확인합니다.
    /// </summary>
    public static bool HasParseMethod(this Type type)
    {
        return type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null) != null ||
               type.GetMethod("TryParse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), type.MakeByRefType() }, null) != null;
    }
    
    /// <summary>
    /// 주어진 타입에 대해 Parse 또는 TryParse 메서드를 사용해 문자열 변환을 시도합니다.
    /// </summary>
    private static bool TryParseString(string sourceString, Type targetType, out object? result)
    {
        result = null;
        
        try 
        {
            // TryParse 메서드 시도
            var tryParseMethod = targetType.GetMethod("TryParse", 
                BindingFlags.Public | BindingFlags.Static, 
                null, 
                new[] { typeof(string), targetType.MakeByRefType() }, 
                null);
                
            if (tryParseMethod != null)
            {
                var parameters = new object?[] { sourceString, null };
                var success = (bool)tryParseMethod.Invoke(null, parameters)!;
                if (success)
                {
                    result = parameters[1];
                    return true;
                }
                return false;
            }
            
            // Parse 메서드 시도
            var parseMethod = targetType.GetMethod("Parse", 
                BindingFlags.Public | BindingFlags.Static, 
                null, 
                new[] { typeof(string) }, 
                null);
                
            if (parseMethod != null)
            {
                result = parseMethod.Invoke(null, new object[] { sourceString });
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
    /// 주어진 타입에 대해 명시적 변환 연산자를 시도합니다.
    /// </summary>
    private static bool TryExplicitConversion(object sourceValue, Type sourceType, Type targetType, out object? result)
    {
        result = null;
        
        try
        {
            // 대상 타입에서 정의된 명시적 변환 연산자 확인
            var targetExplicitOp = targetType.GetMethod("op_Explicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sourceType },
                null);

            if (targetExplicitOp != null)
            {
                result = targetExplicitOp.Invoke(null, new[] { sourceValue });
                return true;
            }
            
            // 소스 타입에서 정의된 명시적 변환 연산자 확인
            var sourceExplicitOp = sourceType.GetMethod("op_Explicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sourceType },
                null);

            if (sourceExplicitOp != null && sourceExplicitOp.ReturnType == targetType)
            {
                result = sourceExplicitOp.Invoke(null, new[] { sourceValue });
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
    /// TypeConverter를 사용하여 변환을 시도합니다.
    /// </summary>
    private static bool TryTypeConverter(object sourceValue, Type targetType, out object? result)
    {
        result = null;
        
        try
        {
            // TypeConverter 사용
            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(sourceValue.GetType()))
            {
                result = converter.ConvertFrom(sourceValue);
                return true;
            }
            
            // 소스 타입에서 대상 타입으로의 변환 시도
            converter = TypeDescriptor.GetConverter(sourceValue.GetType());
            if (converter.CanConvertTo(targetType))
            {
                result = converter.ConvertTo(sourceValue, targetType);
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
    /// 생성자를 통한 변환을 시도합니다.
    /// </summary>
    private static bool TryConstructorConversion(object sourceValue, Type sourceType, Type targetType, out object? result)
    {
        result = null;
        
        try
        {
            // 소스 타입을 매개변수로 받는 생성자 확인
            var constructor = targetType.GetConstructor(new[] { sourceType });
            if (constructor != null)
            {
                result = constructor.Invoke(new[] { sourceValue });
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

            // 3. 소스 타입에서 정의된 암시적 변환 연산자 확인
            var sourceImplicitOp = sourceType.GetMethod("op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sourceType },
                null);

            if (sourceImplicitOp != null && sourceImplicitOp.ReturnType == targetType)
            {
                result = (T)sourceImplicitOp.Invoke(null, new[] { sourceValue })!;
                return true;
            }
            
            // 4. 대상 타입에서 정의된 암시적 변환 연산자 확인
            var targetImplicitOp = targetType.GetMethod("op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sourceType },
                null);
                
            if (targetImplicitOp != null && targetImplicitOp.ReturnType == targetType)
            {
                result = (T)targetImplicitOp.Invoke(null, new[] { sourceValue })!;
                return true;
            }
            
            // 5. 명시적 변환 연산자 확인
            if (TryExplicitConversion(sourceValue, sourceType, targetType, out var explicitResult))
            {
                result = (T)explicitResult!;
                return true;
            }
            
            // 6. 생성자를 통한 변환 시도
            if (TryConstructorConversion(sourceValue, sourceType, targetType, out var constructedResult))
            {
                result = (T)constructedResult!;
                return true;
            }

            // 7. 문자열 변환 시도 (Parse/TryParse)
            if (sourceValue is string sourceString && TryParseString(sourceString, targetType, out var parsedResult))
            {
                result = (T)parsedResult!;
                return true;
            }
            
            // 8. TypeConverter 사용
            if (TryTypeConverter(sourceValue, targetType, out var convertedResult))
            {
                result = (T)convertedResult!;
                return true;
            }

            // 9. 문자열로 변환
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

            // 3. 소스 타입에서 정의된 암시적 변환 연산자 확인
            var sourceImplicitOp = sourceType.GetMethod("op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sourceType },
                null);

            if (sourceImplicitOp != null && sourceImplicitOp.ReturnType == targetType)
            {
                return sourceImplicitOp.Invoke(null, new[] { sourceValue });
            }
            
            // 4. 대상 타입에서 정의된 암시적 변환 연산자 확인
            var targetImplicitOp = targetType.GetMethod("op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sourceType },
                null);
                
            if (targetImplicitOp != null && targetImplicitOp.ReturnType == targetType)
            {
                return targetImplicitOp.Invoke(null, new[] { sourceValue });
            }
            
            // 5. 명시적 변환 연산자 확인
            if (TryExplicitConversion(sourceValue, sourceType, targetType, out var explicitResult))
            {
                return explicitResult;
            }
            
            // 6. 생성자를 통한 변환 시도
            if (TryConstructorConversion(sourceValue, sourceType, targetType, out var constructedResult))
            {
                return constructedResult;
            }

            // 7. 문자열 변환 시도 (Parse/TryParse)
            if (sourceValue is string sourceString && TryParseString(sourceString, targetType, out var parsedResult))
            {
                return parsedResult;
            }
            
            // 8. TypeConverter 사용
            if (TryTypeConverter(sourceValue, targetType, out var convertedResult))
            {
                return convertedResult;
            }

            // 9. 문자열로 변환
            if (targetType == typeof(string))
            {
                return sourceValue.ToString();
            }
            
            // 10. 마지막 수단으로 Convert.ChangeType 시도
            try
            {
                return Convert.ChangeType(sourceValue, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
    }
}
