using System.Windows;
using WPFNode.Interfaces;
using WPFNode.ViewModels.Interfaces;

namespace WPFNode.Services;

/// <summary>
/// WPF 환경에서 PropertyControlProviderRegistry를 사용하는 컨트롤 팩토리
/// </summary>
public class WPFPropertyControlFactory : IPropertyControlFactory
{
    /// <summary>
    /// PropertyControlProviderRegistry를 사용하여 컨트롤을 생성합니다.
    /// </summary>
    public FrameworkElement CreateControl(INodeProperty property)
    {
        return PropertyControlProviderRegistry.CreateControl(property);
    }
} 