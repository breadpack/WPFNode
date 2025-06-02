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
                var parameters = new object[] { sourceString, Activator.CreateInstance(targetType)! };
                if ((bool)tryParseMethod.Invoke(null, parameters)!)
                {
                    result = parameters[1];
                    return true;
                }
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
                if (sourceValue is string sourceString && string.IsNullOrEmpty(sourceString)) {
                    result = Activator.CreateInstance(targetType);
                    return true;
                }
                
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
    /// 캐시된 변환 전략을 실행합니다.
    /// </summary>
    private static bool ExecuteCachedConversion<T>(object? sourceValue, ConversionCacheEntry cacheEntry, out T? result)
    {
        result = default;
        if (sourceValue == null) return false;

        try
        {
            var sourceType = sourceValue.GetType();
            var targetType = typeof(T);

            switch (cacheEntry.Strategy)
            {
                case ConversionStrategy.Direct:
                    if (sourceValue is T directValue)
                    {
                        result = directValue;
                        return true;
                    }
                    break;

                case ConversionStrategy.Numeric:
                    if (sourceType.IsNumericType() && targetType.IsNumericType())
                    {
                        if (TryNumericConversion(sourceValue, targetType, out var numericResult))
                        {
                            result = (T)numericResult!;
                            return true;
                        }
                    }
                    break;

                case ConversionStrategy.SourceImplicit:
                case ConversionStrategy.TargetImplicit:
                    if (cacheEntry.Method != null)
                    {
                        result = (T)cacheEntry.Method.Invoke(null, new[] { sourceValue })!;
                        return true;
                    }
                    break;

                case ConversionStrategy.Explicit:
                    if (cacheEntry.Method != null)
                    {
                        result = (T)cacheEntry.Method.Invoke(null, new[] { sourceValue })!;
                        return true;
                    }
                    break;

                case ConversionStrategy.Constructor:
                    if (cacheEntry.Constructor != null)
                    {
                        result = (T)cacheEntry.Constructor.Invoke(new[] { sourceValue })!;
                        return true;
                    }
                    break;

                case ConversionStrategy.Parse:
                    if (sourceValue is string sourceString && cacheEntry.Method != null)
                    {
                        if (cacheEntry.Method.Name == "TryParse")
                        {
                            var parameters = new object[] { sourceString, default(T)! };
                            if ((bool)cacheEntry.Method.Invoke(null, parameters)!)
                            {
                                result = (T)parameters[1];
                                return true;
                            }
                        }
                        else if (cacheEntry.Method.Name == "Parse")
                        {
                            result = (T)cacheEntry.Method.Invoke(null, new object[] { sourceString })!;
                            return true;
                        }
                    }
                    break;

                case ConversionStrategy.TypeConverter:
                    if (TryTypeConverter(sourceValue, targetType, out var convertedResult))
                    {
                        result = (T)convertedResult!;
                        return true;
                    }
                    break;

                case ConversionStrategy.ToString:
                    if (targetType == typeof(string))
                    {
                        result = (T)(object)sourceValue.ToString()!;
                        return true;
                    }
                    break;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 변환 전략을 캐시에 저장합니다.
    /// </summary>
    private static void CacheConversionResult(Type sourceType, Type targetType, ConversionStrategy strategy, MethodInfo? method = null, ConstructorInfo? constructor = null)
    {
        var entry = new ConversionCacheEntry(strategy, method, constructor);
        ConversionCache.CacheConversionStrategy(sourceType, targetType, entry);
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

            // 캐시에서 변환 전략 조회
            if (ConversionCache.TryGetConversionStrategy(sourceType, targetType, out var cachedEntry))
            {
                if (ExecuteCachedConversion(sourceValue, cachedEntry, out result))
                {
                    return true;
                }
            }

            // 1. 직접 타입 변환 가능한 경우
            if (sourceValue is T typedValue)
            {
                result = typedValue;
                CacheConversionResult(sourceType, targetType, ConversionStrategy.Direct);
                return true;
            }

            // 2. 숫자 타입 간 변환
            if (sourceType.IsNumericType() && targetType.IsNumericType())
            {
                // 직접 캐스팅을 사용하여 C# 표준 동작 (버림) 유지
                if (TryNumericConversion(sourceValue, targetType, out var numericResult))
                {
                    result = (T)numericResult!;
                    CacheConversionResult(sourceType, targetType, ConversionStrategy.Numeric);
                    return true;
                }
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
                CacheConversionResult(sourceType, targetType, ConversionStrategy.SourceImplicit, sourceImplicitOp);
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
                CacheConversionResult(sourceType, targetType, ConversionStrategy.TargetImplicit, targetImplicitOp);
                return true;
            }
            
            // 5. 명시적 변환 연산자 확인
            if (TryExplicitConversion(sourceValue, sourceType, targetType, out var explicitResult))
            {
                result = (T)explicitResult!;
                // 명시적 변환에서 사용된 메서드 정보를 가져와서 캐시
                var explicitOp = sourceType.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.Static, null, new[] { sourceType }, null) ??
                                targetType.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.Static, null, new[] { sourceType }, null);
                CacheConversionResult(sourceType, targetType, ConversionStrategy.Explicit, explicitOp);
                return true;
            }
            
            // 6. 생성자를 통한 변환 시도
            if (TryConstructorConversion(sourceValue, sourceType, targetType, out var constructedResult))
            {
                result = (T)constructedResult!;
                var constructor = targetType.GetConstructor(new[] { sourceType });
                CacheConversionResult(sourceType, targetType, ConversionStrategy.Constructor, null, constructor);
                return true;
            }

            // 7. 문자열 변환 시도 (Parse/TryParse)
            if (sourceValue is string sourceString && TryParseString(sourceString, targetType, out var parsedResult))
            {
                result = (T)parsedResult!;
                var parseMethod = targetType.GetMethod("TryParse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), targetType.MakeByRefType() }, null) ??
                                 targetType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                CacheConversionResult(sourceType, targetType, ConversionStrategy.Parse, parseMethod);
                return true;
            }
            
            // 8. TypeConverter 사용
            if (TryTypeConverter(sourceValue, targetType, out var convertedResult))
            {
                result = (T)convertedResult!;
                CacheConversionResult(sourceType, targetType, ConversionStrategy.TypeConverter);
                return true;
            }

            // 9. 문자열로 변환
            if (targetType == typeof(string))
            {
                result = (T)(object)sourceValue.ToString()!;
                CacheConversionResult(sourceType, targetType, ConversionStrategy.ToString);
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

            // 캐시에서 변환 전략 조회
            if (ConversionCache.TryGetConversionStrategy(sourceType, targetType, out var cachedEntry))
            {
                if (ExecuteCachedConversionNonGeneric(sourceValue, cachedEntry, targetType, out var cachedResult))
                {
                    return cachedResult;
                }
            }

            // 1. 직접 타입 변환 가능한 경우
            if (targetType.IsAssignableFrom(sourceType))
            {
                CacheConversionResult(sourceType, targetType, ConversionStrategy.Direct);
                return sourceValue;
            }

            // 2. 숫자 타입 간 변환
            if (sourceType.IsNumericType() && targetType.IsNumericType())
            {
                // 직접 캐스팅을 사용하여 C# 표준 동작 (버림) 유지
                if (TryNumericConversion(sourceValue, targetType, out var numericResult))
                {
                    CacheConversionResult(sourceType, targetType, ConversionStrategy.Numeric);
                    return numericResult;
                }
            }

            // 3. 소스 타입에서 정의된 암시적 변환 연산자 확인
            var sourceImplicitOp = sourceType.GetMethod("op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sourceType },
                null);

            if (sourceImplicitOp != null && sourceImplicitOp.ReturnType == targetType)
            {
                var result = sourceImplicitOp.Invoke(null, new[] { sourceValue });
                CacheConversionResult(sourceType, targetType, ConversionStrategy.SourceImplicit, sourceImplicitOp);
                return result;
            }
            
            // 4. 대상 타입에서 정의된 암시적 변환 연산자 확인
            var targetImplicitOp = targetType.GetMethod("op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sourceType },
                null);
                
            if (targetImplicitOp != null && targetImplicitOp.ReturnType == targetType)
            {
                var result = targetImplicitOp.Invoke(null, new[] { sourceValue });
                CacheConversionResult(sourceType, targetType, ConversionStrategy.TargetImplicit, targetImplicitOp);
                return result;
            }
            
            // 5. 명시적 변환 연산자 확인
            if (TryExplicitConversion(sourceValue, sourceType, targetType, out var explicitResult))
            {
                var explicitOp = sourceType.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.Static, null, new[] { sourceType }, null) ??
                                targetType.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.Static, null, new[] { sourceType }, null);
                CacheConversionResult(sourceType, targetType, ConversionStrategy.Explicit, explicitOp);
                return explicitResult;
            }
            
            // 6. 생성자를 통한 변환 시도
            if (TryConstructorConversion(sourceValue, sourceType, targetType, out var constructedResult))
            {
                var constructor = targetType.GetConstructor(new[] { sourceType });
                CacheConversionResult(sourceType, targetType, ConversionStrategy.Constructor, null, constructor);
                return constructedResult;
            }

            // 7. 문자열 변환 시도 (Parse/TryParse)
            if (sourceValue is string sourceString && TryParseString(sourceString, targetType, out var parsedResult))
            {
                var parseMethod = targetType.GetMethod("TryParse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), targetType.MakeByRefType() }, null) ??
                                 targetType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                CacheConversionResult(sourceType, targetType, ConversionStrategy.Parse, parseMethod);
                return parsedResult;
            }
            
            // 8. TypeConverter 사용
            if (TryTypeConverter(sourceValue, targetType, out var convertedResult))
            {
                CacheConversionResult(sourceType, targetType, ConversionStrategy.TypeConverter);
                return convertedResult;
            }

            // 9. 문자열로 변환
            if (targetType == typeof(string))
            {
                var result = sourceValue.ToString();
                CacheConversionResult(sourceType, targetType, ConversionStrategy.ToString);
                return result;
            }
            
            // 10. 마지막 수단으로 Convert.ChangeType 시도
            try
            {
                var result = Convert.ChangeType(sourceValue, targetType, CultureInfo.InvariantCulture);
                CacheConversionResult(sourceType, targetType, ConversionStrategy.Numeric);
                return result;
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

    /// <summary>
    /// 캐시된 변환 전략을 실행합니다 (비제네릭 버전).
    /// </summary>
    private static bool ExecuteCachedConversionNonGeneric(object? sourceValue, ConversionCacheEntry cacheEntry, Type targetType, out object? result)
    {
        result = null;
        if (sourceValue == null) return false;

        try
        {
            var sourceType = sourceValue.GetType();

            switch (cacheEntry.Strategy)
            {
                case ConversionStrategy.Direct:
                    if (targetType.IsAssignableFrom(sourceType))
                    {
                        result = sourceValue;
                        return true;
                    }
                    break;

                case ConversionStrategy.Numeric:
                    if (sourceType.IsNumericType() && targetType.IsNumericType())
                    {
                        if (TryNumericConversion(sourceValue, targetType, out result))
                        {
                            return true;
                        }
                    }
                    break;

                case ConversionStrategy.SourceImplicit:
                case ConversionStrategy.TargetImplicit:
                    if (cacheEntry.Method != null)
                    {
                        result = cacheEntry.Method.Invoke(null, new[] { sourceValue });
                        return true;
                    }
                    break;

                case ConversionStrategy.Explicit:
                    if (cacheEntry.Method != null)
                    {
                        result = cacheEntry.Method.Invoke(null, new[] { sourceValue });
                        return true;
                    }
                    break;

                case ConversionStrategy.Constructor:
                    if (cacheEntry.Constructor != null)
                    {
                        result = cacheEntry.Constructor.Invoke(new[] { sourceValue });
                        return true;
                    }
                    break;

                case ConversionStrategy.Parse:
                    if (sourceValue is string sourceString && cacheEntry.Method != null)
                    {
                        if (cacheEntry.Method.Name == "TryParse")
                        {
                            var parameters = new object[] { sourceString, Activator.CreateInstance(targetType)! };
                            if ((bool)cacheEntry.Method.Invoke(null, parameters)!)
                            {
                                result = parameters[1];
                                return true;
                            }
                        }
                        else if (cacheEntry.Method.Name == "Parse")
                        {
                            result = cacheEntry.Method.Invoke(null, new object[] { sourceString });
                            return true;
                        }
                    }
                    break;

                case ConversionStrategy.TypeConverter:
                    if (TryTypeConverter(sourceValue, targetType, out result))
                    {
                        return true;
                    }
                    break;

                case ConversionStrategy.ToString:
                    if (targetType == typeof(string))
                    {
                        result = sourceValue.ToString();
                        return true;
                    }
                    break;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 숫자 타입 간 직접 캐스팅을 수행합니다. (C# 표준 동작 유지)
    /// </summary>
    private static bool TryNumericConversion(object sourceValue, Type targetType, out object? result)
    {
        result = null;
        
        try
        {
            // 각 소스 타입별로 직접 처리
            switch (sourceValue)
            {
                case byte b:
                    result = ConvertFromByte(b, targetType);
                    break;
                case sbyte sb:
                    result = ConvertFromSByte(sb, targetType);
                    break;
                case short s:
                    result = ConvertFromShort(s, targetType);
                    break;
                case ushort us:
                    result = ConvertFromUShort(us, targetType);
                    break;
                case int i:
                    result = ConvertFromInt(i, targetType);
                    break;
                case uint ui:
                    result = ConvertFromUInt(ui, targetType);
                    break;
                case long l:
                    result = ConvertFromLong(l, targetType);
                    break;
                case ulong ul:
                    result = ConvertFromULong(ul, targetType);
                    break;
                case float f:
                    result = ConvertFromFloat(f, targetType);
                    break;
                case double d:
                    result = ConvertFromDouble(d, targetType);
                    break;
                case decimal dec:
                    result = ConvertFromDecimal(dec, targetType);
                    break;
                default:
                    return false;
            }
                
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    private static object? ConvertFromByte(byte value, Type targetType)
    {
        if (targetType == typeof(byte)) return value;
        if (targetType == typeof(sbyte)) return (sbyte)value;
        if (targetType == typeof(short)) return (short)value;
        if (targetType == typeof(ushort)) return (ushort)value;
        if (targetType == typeof(int)) return (int)value;
        if (targetType == typeof(uint)) return (uint)value;
        if (targetType == typeof(long)) return (long)value;
        if (targetType == typeof(ulong)) return (ulong)value;
        if (targetType == typeof(float)) return (float)value;
        if (targetType == typeof(double)) return (double)value;
        if (targetType == typeof(decimal)) return (decimal)value;
        return null;
    }

    private static object? ConvertFromSByte(sbyte value, Type targetType)
    {
        if (targetType == typeof(byte)) return (byte)value;
        if (targetType == typeof(sbyte)) return value;
        if (targetType == typeof(short)) return (short)value;
        if (targetType == typeof(ushort)) return (ushort)value;
        if (targetType == typeof(int)) return (int)value;
        if (targetType == typeof(uint)) return (uint)value;
        if (targetType == typeof(long)) return (long)value;
        if (targetType == typeof(ulong)) return (ulong)value;
        if (targetType == typeof(float)) return (float)value;
        if (targetType == typeof(double)) return (double)value;
        if (targetType == typeof(decimal)) return (decimal)value;
        return null;
    }

    private static object? ConvertFromShort(short value, Type targetType)
    {
        if (targetType == typeof(byte)) return (byte)value;
        if (targetType == typeof(sbyte)) return (sbyte)value;
        if (targetType == typeof(short)) return value;
        if (targetType == typeof(ushort)) return (ushort)value;
        if (targetType == typeof(int)) return (int)value;
        if (targetType == typeof(uint)) return (uint)value;
        if (targetType == typeof(long)) return (long)value;
        if (targetType == typeof(ulong)) return (ulong)value;
        if (targetType == typeof(float)) return (float)value;
        if (targetType == typeof(double)) return (double)value;
        if (targetType == typeof(decimal)) return (decimal)value;
        return null;
    }

    private static object? ConvertFromUShort(ushort value, Type targetType)
    {
        if (targetType == typeof(byte)) return (byte)value;
        if (targetType == typeof(sbyte)) return (sbyte)value;
        if (targetType == typeof(short)) return (short)value;
        if (targetType == typeof(ushort)) return value;
        if (targetType == typeof(int)) return (int)value;
        if (targetType == typeof(uint)) return (uint)value;
        if (targetType == typeof(long)) return (long)value;
        if (targetType == typeof(ulong)) return (ulong)value;
        if (targetType == typeof(float)) return (float)value;
        if (targetType == typeof(double)) return (double)value;
        if (targetType == typeof(decimal)) return (decimal)value;
        return null;
    }

    private static object? ConvertFromInt(int value, Type targetType)
    {
        if (targetType == typeof(byte)) return (byte)value;
        if (targetType == typeof(sbyte)) return (sbyte)value;
        if (targetType == typeof(short)) return (short)value;
        if (targetType == typeof(ushort)) return (ushort)value;
        if (targetType == typeof(int)) return value;
        if (targetType == typeof(uint)) return (uint)value;
        if (targetType == typeof(long)) return (long)value;
        if (targetType == typeof(ulong)) return (ulong)value;
        if (targetType == typeof(float)) return (float)value;
        if (targetType == typeof(double)) return (double)value;
        if (targetType == typeof(decimal)) return (decimal)value;
        return null;
    }

    private static object? ConvertFromUInt(uint value, Type targetType)
    {
        if (targetType == typeof(byte)) return (byte)value;
        if (targetType == typeof(sbyte)) return (sbyte)value;
        if (targetType == typeof(short)) return (short)value;
        if (targetType == typeof(ushort)) return (ushort)value;
        if (targetType == typeof(int)) return (int)value;
        if (targetType == typeof(uint)) return value;
        if (targetType == typeof(long)) return (long)value;
        if (targetType == typeof(ulong)) return (ulong)value;
        if (targetType == typeof(float)) return (float)value;
        if (targetType == typeof(double)) return (double)value;
        if (targetType == typeof(decimal)) return (decimal)value;
        return null;
    }

    private static object? ConvertFromLong(long value, Type targetType)
    {
        if (targetType == typeof(byte)) return (byte)value;
        if (targetType == typeof(sbyte)) return (sbyte)value;
        if (targetType == typeof(short)) return (short)value;
        if (targetType == typeof(ushort)) return (ushort)value;
        if (targetType == typeof(int)) return (int)value;
        if (targetType == typeof(uint)) return (uint)value;
        if (targetType == typeof(long)) return value;
        if (targetType == typeof(ulong)) return (ulong)value;
        if (targetType == typeof(float)) return (float)value;
        if (targetType == typeof(double)) return (double)value;
        if (targetType == typeof(decimal)) return (decimal)value;
        return null;
    }

    private static object? ConvertFromULong(ulong value, Type targetType)
    {
        if (targetType == typeof(byte)) return (byte)value;
        if (targetType == typeof(sbyte)) return (sbyte)value;
        if (targetType == typeof(short)) return (short)value;
        if (targetType == typeof(ushort)) return (ushort)value;
        if (targetType == typeof(int)) return (int)value;
        if (targetType == typeof(uint)) return (uint)value;
        if (targetType == typeof(long)) return (long)value;
        if (targetType == typeof(ulong)) return value;
        if (targetType == typeof(float)) return (float)value;
        if (targetType == typeof(double)) return (double)value;
        if (targetType == typeof(decimal)) return (decimal)value;
        return null;
    }

    private static object? ConvertFromFloat(float value, Type targetType)
    {
        if (targetType == typeof(byte)) return (byte)value;
        if (targetType == typeof(sbyte)) return (sbyte)value;
        if (targetType == typeof(short)) return (short)value;
        if (targetType == typeof(ushort)) return (ushort)value;
        if (targetType == typeof(int)) return (int)value;
        if (targetType == typeof(uint)) return (uint)value;
        if (targetType == typeof(long)) return (long)value;
        if (targetType == typeof(ulong)) return (ulong)value;
        if (targetType == typeof(float)) return value;
        if (targetType == typeof(double)) return (double)value;
        if (targetType == typeof(decimal)) return (decimal)value;
        return null;
    }

    private static object? ConvertFromDouble(double value, Type targetType)
    {
        if (targetType == typeof(byte)) return (byte)value;
        if (targetType == typeof(sbyte)) return (sbyte)value;
        if (targetType == typeof(short)) return (short)value;
        if (targetType == typeof(ushort)) return (ushort)value;
        if (targetType == typeof(int)) return (int)value; // 여기서 버림 발생
        if (targetType == typeof(uint)) return (uint)value;
        if (targetType == typeof(long)) return (long)value;
        if (targetType == typeof(ulong)) return (ulong)value;
        if (targetType == typeof(float)) return (float)value;
        if (targetType == typeof(double)) return value;
        if (targetType == typeof(decimal)) return (decimal)value;
        return null;
    }

    private static object? ConvertFromDecimal(decimal value, Type targetType)
    {
        if (targetType == typeof(byte)) return (byte)value;
        if (targetType == typeof(sbyte)) return (sbyte)value;
        if (targetType == typeof(short)) return (short)value;
        if (targetType == typeof(ushort)) return (ushort)value;
        if (targetType == typeof(int)) return (int)value;
        if (targetType == typeof(uint)) return (uint)value;
        if (targetType == typeof(long)) return (long)value;
        if (targetType == typeof(ulong)) return (ulong)value;
        if (targetType == typeof(float)) return (float)value;
        if (targetType == typeof(double)) return (double)value;
        if (targetType == typeof(decimal)) return value;
        return null;
    }
}
