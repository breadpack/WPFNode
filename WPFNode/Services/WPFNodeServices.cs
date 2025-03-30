using System;
using System.Windows;
using Microsoft.Extensions.Logging;
using WPFNode.Interfaces;
using WPFNode.ViewModels;

namespace WPFNode.Services;

/// <summary>
/// WPF 프로젝트에서 사용하는 서비스 초기화 및 등록 클래스
/// </summary>
public static class WPFNodeServices
{
    private static readonly Lazy<INodeUIService> _uiService = 
        new(() => new NodeUIService(NodeServices.ModelService));
        
    private static readonly Lazy<WPFNodePluginService> _combinedService =
        new(() => new WPFNodePluginService(NodeServices.ModelService, UIService));
        
    private static readonly Lazy<WPFPropertyControlFactory> _propertyControlFactory =
        new(() => new WPFPropertyControlFactory());
        
    static WPFNodeServices()
    {
        // NodeServices의 플러그인 로드 이벤트 구독
        NodeServices.PluginPathLoaded += OnPluginPathLoaded;
    }
    
    // 플러그인 경로가 로드되었을 때 호출되는 핸들러
    private static void OnPluginPathLoaded(string pluginPath)
    {
        // UI 측에서 플러그인 로드
        UIService.LoadExternalUIPlugins(pluginPath);
    }
    
    /// <summary>
    /// WPF UI 기능을 포함한 노드 플러그인 서비스
    /// </summary>
    public static WPFNodePluginService CombinedPluginService => _combinedService.Value;
    
    /// <summary>
    /// UI 관련 노드 서비스
    /// </summary>
    public static INodeUIService UIService => _uiService.Value;

    /// <summary>
    /// 속성 컨트롤 프로바이더 접근을 위한 메서드
    /// </summary>
    public static FrameworkElement CreatePropertyControl(INodeProperty property)
    {
        return PropertyControlProviderRegistry.CreateControl(property);
    }
    
    /// <summary>
    /// WPF 서비스 초기화
    /// </summary>
    public static void Initialize()
    {
        // 명시적 초기화가 필요한 경우 여기에 코드 추가
        // UIService를 미리 초기화하여 이벤트 핸들러가 활성화되도록 함
        _ = UIService;
        
        // 속성 컨트롤 프로바이더 초기화
        PropertyControlProviderRegistry.RegisterProviders();
        
        // WPF 컨트롤 팩토리를 ViewModel에 등록
        ViewModelServices.PropertyControlFactory = _propertyControlFactory.Value;
    }
} 