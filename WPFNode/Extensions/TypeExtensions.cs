using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WPFNode.Extensions;

public static class TypeExtensions
{
    /// <summary>
    /// 타입의 사용자 친화적인 이름을 반환합니다.
    /// </summary>
    /// <param name="type">타입</param>
    /// <returns>사용자 친화적인 이름</returns>
    public static string GetUserFriendlyName(this Type type)
    {
        if (type == null)
            return "null";

        // 제네릭 타입이 아닌 경우
        if (!type.IsGenericType)
            return type.Name;

        // 제네릭 타입 정의인 경우
        if (type.IsGenericTypeDefinition)
        {
            var name = type.Name.Split('`')[0];
            var typeParams = type.GetGenericArguments();
            return $"{name}<{string.Join(",", typeParams.Select(p => p.Name))}>";
        }

        // 제네릭 타입인 경우
        var baseName = type.Name.Split('`')[0];
        var arguments = type.GetGenericArguments();
        var argNames = arguments.Select(arg => arg.GetUserFriendlyName());
        return $"{baseName}<{string.Join(",", argNames)}>";
    }

    /// <summary>
    /// 타입의 전체 사용자 친화적인 이름을 반환합니다 (네임스페이스 포함).
    /// </summary>
    /// <param name="type">타입</param>
    /// <returns>전체 사용자 친화적인 이름</returns>
    public static string GetUserFriendlyFullName(this Type type)
    {
        if (type == null)
            return "null";

        var name = type.GetUserFriendlyName();
        var ns = type.Namespace;

        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    /// <summary>
    /// 주어진 타입이 IDictionary<TKey, TValue> 인터페이스를 구현하는 경우,
    /// 해당 키와 값의 타입을 튜플로 반환합니다. 구현하지 않으면 null을 반환합니다.
    /// </summary>
    public static (Type KeyType, Type ValueType)? GetDictionaryKeyValueTypes(this Type type)
    {
        if (type == null) return null;

        // 타입 자체가 IDictionary<,> 인 경우
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
        {
            var genericArgs = type.GetGenericArguments();
            return (genericArgs[0], genericArgs[1]);
        }

        // 타입이 IDictionary<,>를 구현하는 인터페이스를 찾음
        var dictionaryInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictionaryInterface != null)
        {
            var genericArgs = dictionaryInterface.GetGenericArguments();
            return (genericArgs[0], genericArgs[1]);
        }

        return null; // IDictionary<,>를 구현하지 않음
    }

    /// <summary>
    /// 메서드의 사용자 친화적인 이름을 반환합니다.
    /// </summary>
    /// <param name="methodInfo">메서드 정보</param>
    /// <returns>사용자 친화적인 메서드 이름 (파라미터 타입 포함)</returns>
    public static string GetUserFriendlyName(this MethodInfo methodInfo)
    {
        if (methodInfo == null)
            return "null";

        var parameters = methodInfo.GetParameters();
        if (parameters.Length == 0)
            return $"{methodInfo.Name}()";

        var paramNames = parameters.Select(p => $"{p.ParameterType.GetUserFriendlyName()} {p.Name}");
        return $"{methodInfo.Name}({string.Join(", ", paramNames)})";
    }
    
    /// <summary>
    /// 메서드의 사용자 친화적인 시그니처를 반환합니다.
    /// </summary>
    /// <param name="methodInfo">메서드 정보</param>
    /// <returns>반환 타입을 포함한 사용자 친화적인 메서드 시그니처</returns>
    public static string GetUserFriendlySignature(this MethodInfo methodInfo)
    {
        if (methodInfo == null)
            return "null";

        var returnTypeName = methodInfo.ReturnType.GetUserFriendlyName();
        var methodName = methodInfo.GetUserFriendlyName();
        
        return $"{returnTypeName} {methodName}";
    }
    
    /// <summary>
    /// PropertyInfo의 사용자 친화적인 이름을 반환합니다.
    /// </summary>
    /// <param name="propertyInfo">프로퍼티 정보</param>
    /// <returns>사용자 친화적인 프로퍼티 이름 (타입 포함)</returns>
    public static string GetUserFriendlyName(this PropertyInfo propertyInfo)
    {
        if (propertyInfo == null)
            return "null";
            
        var typeName = propertyInfo.PropertyType.GetUserFriendlyName();
        return $"{typeName} {propertyInfo.Name}";
    }
    
    /// <summary>
    /// FieldInfo의 사용자 친화적인 이름을 반환합니다.
    /// </summary>
    /// <param name="fieldInfo">필드 정보</param>
    /// <returns>사용자 친화적인 필드 이름 (타입 포함)</returns>
    public static string GetUserFriendlyName(this FieldInfo fieldInfo)
    {
        if (fieldInfo == null)
            return "null";
            
        var typeName = fieldInfo.FieldType.GetUserFriendlyName();
        return $"{typeName} {fieldInfo.Name}";
    }
}
