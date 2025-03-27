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
} 