using System.Windows;
using WPFNode.Interfaces;

namespace WPFNode.ViewModels.Interfaces;

/// <summary>
/// 속성 컨트롤을 생성하는 팩토리 인터페이스
/// </summary>
public interface IPropertyControlFactory
{
    /// <summary>
    /// 속성에 대한 컨트롤을 생성합니다.
    /// </summary>
    /// <param name="property">노드 속성</param>
    /// <returns>생성된 UI 컨트롤</returns>
    FrameworkElement CreateControl(INodeProperty property);
} 