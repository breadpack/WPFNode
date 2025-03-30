using WPFNode.ViewModels.Interfaces;

namespace WPFNode.ViewModels;

/// <summary>
/// ViewModel 계층에서 사용하는 서비스 로케이터
/// </summary>
public static class ViewModelServices
{
    private static IPropertyControlFactory _propertyControlFactory = DefaultPropertyControlFactory.Instance;
    
    /// <summary>
    /// 속성 컨트롤 팩토리 서비스
    /// </summary>
    public static IPropertyControlFactory PropertyControlFactory
    {
        get => _propertyControlFactory;
        set => _propertyControlFactory = value ?? DefaultPropertyControlFactory.Instance;
    }
    
    /// <summary>
    /// 서비스 초기화
    /// </summary>
    public static void Initialize()
    {
        // 필요시 초기화 코드 추가
    }
} 