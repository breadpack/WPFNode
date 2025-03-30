using System.IO;
using System.Reflection;
using WPFNode.Interfaces;

namespace WPFNode.Services;

/// <summary>
/// 모델 프로젝트에서 사용하는 서비스 클래스
/// WPF 의존성이 없는 순수 모델 서비스만 제공합니다.
/// </summary>
public static class NodeServices
{
    // 플러그인 로드 시 발생하는 이벤트 (UI 계층에서 구독 가능)
    public static event Action<string>? PluginPathLoaded;
    
    // INodeModelService 구현은 NodeModelService를 사용
    private static readonly Lazy<NodeModelService> _modelService = 
        new(() => new NodeModelService());
    
    // 명령 서비스는 ModelService에 의존 (UI 기능 불필요)
    private static readonly Lazy<INodeCommandService> _commandService = 
        new(() => new NodeCommandService(_modelService.Value));

    // Model 서비스 (INodeModelService 구현)
    public static INodeModelService ModelService => _modelService.Value;
    
    // 명령 서비스
    public static INodeCommandService CommandService => _commandService.Value;

    // 초기화 메서드
    public static void Initialize(string pluginPath)
    {
        // 외부 플러그인 로드 (Model 부분만)
        if (!string.IsNullOrEmpty(pluginPath) && Directory.Exists(pluginPath))
        {
            ModelService.LoadPlugins(pluginPath);
            
            // 플러그인 로드 이벤트 발생 (UI 계층에서 구독 가능)
            PluginPathLoaded?.Invoke(pluginPath);
        }
    }
} 