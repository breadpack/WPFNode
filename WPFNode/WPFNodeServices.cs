using System;
using System.Windows;
using Microsoft.Extensions.Logging;
using WPFNode.Interfaces;
using WPFNode.Services;

namespace WPFNode;

/// <summary>
/// WPF 프로젝트에서 사용하는 서비스 클래스
/// 다양한 WPF 관련 서비스를 제공합니다.
/// </summary>
public static class WPFNodeServices
{
    // Model 서비스는 NodeServices에서 제공
    public static INodeModelService ModelService => NodeServices.ModelService;
    
    // 명령 서비스 역시 NodeServices에서 제공 
    public static INodeCommandService CommandService => NodeServices.CommandService;
    
    // UI 서비스는 별도로 제공 (WPF 컨트롤 관련 기능)
    private static readonly Lazy<INodeUIService> _uiService = 
        new(() => new NodeUIService(ModelService, null));
    
    // UI 서비스 공개 속성
    public static INodeUIService UIService => _uiService.Value;
    
    // 어플리케이션 초기화 메서드
    static WPFNodeServices()
    {
        // 필요한 초기화 수행
    }
    
    /// <summary>
    /// 응용 프로그램 시작 시 호출되는 초기화 메서드
    /// </summary>
    public static void Initialize(string? pluginPath = null)
    {
        // 노드 모델 서비스 초기화 (기본 노드 타입 로드)
        NodeServices.Initialize(pluginPath ?? string.Empty);
        
        // UI 관련 플러그인도 로드
        if (!string.IsNullOrEmpty(pluginPath))
        {
            UIService.LoadExternalUIPlugins(pluginPath);
        }
    }
} 