using System;
using System.Collections.Generic;
using System.Linq;
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
}
