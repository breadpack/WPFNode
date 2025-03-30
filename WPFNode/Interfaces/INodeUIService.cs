using System;
using System.Windows;
using WPFNode.Models;

namespace WPFNode.Interfaces;

/// <summary>
/// WPF UI 관련 노드 서비스 인터페이스
/// </summary>
public interface INodeUIService
{
    // UI 관련 메서드만 포함
    Style? FindNodeStyle(Type nodeType);
    
    // 외부 UI 플러그인 로드
    void LoadExternalUIPlugins(string pluginPath);
} 