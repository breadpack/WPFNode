using System.Windows;
using System.Windows.Controls;
using WPFNode.Interfaces;
using WPFNode.ViewModels.Interfaces;

namespace WPFNode.ViewModels;

/// <summary>
/// 기본 텍스트박스를 생성하는 속성 컨트롤 팩토리
/// </summary>
public class DefaultPropertyControlFactory : IPropertyControlFactory
{
    private static readonly DefaultPropertyControlFactory _instance = new();
    
    /// <summary>
    /// 싱글톤 인스턴스
    /// </summary>
    public static DefaultPropertyControlFactory Instance => _instance;
    
    private DefaultPropertyControlFactory() { }
    
    /// <summary>
    /// 속성에 대한 기본 텍스트박스 컨트롤을 생성합니다.
    /// </summary>
    public FrameworkElement CreateControl(INodeProperty property)
    {
        // 기본 구현은 단순히 TextBox를 반환
        return new TextBox();
    }
} 