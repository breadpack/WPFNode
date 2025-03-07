using System.Windows;

namespace WPFNode.Interfaces;

public interface INodePlugin
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
