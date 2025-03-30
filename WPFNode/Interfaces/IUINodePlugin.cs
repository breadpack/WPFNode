using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace WPFNode.Interfaces;

/// <summary>
/// UI 관련 노드 플러그인 인터페이스
/// </summary>
public interface IUINodePlugin
{
    /// <summary>
    /// 노드 스타일을 제공합니다.
    /// </summary>
    /// <returns>노드 스타일 리소스 사전 목록</returns>
    IEnumerable<ResourceDictionary> GetNodeStyles();
    
    /// <summary>
    /// 속성 컨트롤 제공자를 제공합니다.
    /// </summary>
    /// <returns>PropertyControlProvider 목록</returns>
    IEnumerable<IPropertyControlProvider> GetPropertyControlProviders() => Enumerable.Empty<IPropertyControlProvider>();
} 