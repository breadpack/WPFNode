using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Extensions.Logging;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Services;

/// <summary>
/// WPF 노드 플러그인 서비스 구현
/// </summary>
public class WPFNodePluginService : INodeUIService
{
    private readonly INodeModelService _modelService;
    private readonly INodeUIService _uiService;
    private readonly ILogger<WPFNodePluginService>? _logger;
    private bool _isDisposed;
    
    public WPFNodePluginService(INodeModelService modelService, INodeUIService uiService, ILogger<WPFNodePluginService>? logger = null)
    {
        _modelService = modelService;
        _uiService = uiService;
        _logger = logger;
    }

    #region INodeModelService 구현
    
    public IReadOnlyCollection<Type> NodeTypes => _modelService.NodeTypes;
    
    public void LoadPlugins(string pluginPath)
    {
        // 모델 서비스의 플러그인 로드
        _modelService.LoadPlugins(pluginPath);
        
        // UI 서비스의 플러그인 로드 (직접 호출)
        _uiService.LoadExternalUIPlugins(pluginPath);
    }
    
    public void RegisterNodeType(Type nodeType) => _modelService.RegisterNodeType(nodeType);
    public INode CreateNode(Type nodeType) => _modelService.CreateNode(nodeType);
    public IEnumerable<string> GetCategories() => _modelService.GetCategories();
    public IEnumerable<Type> GetNodeTypesByCategory(string category) => _modelService.GetNodeTypesByCategory(category);
    public NodeMetadata GetNodeMetadata(Type nodeType) => _modelService.GetNodeMetadata(nodeType);
    public IEnumerable<NodeMetadata> GetNodeMetadataByCategory(string category) => _modelService.GetNodeMetadataByCategory(category);
    public IEnumerable<NodeMetadata> GetAllNodeMetadata() => _modelService.GetAllNodeMetadata();
    
    #endregion
    
    #region INodeUIService 구현
    
    public Style? FindNodeStyle(Type nodeType) => _uiService.FindNodeStyle(nodeType);
    public void LoadExternalUIPlugins(string pluginPath) => _uiService.LoadExternalUIPlugins(pluginPath);
    
    #endregion

    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (_modelService is IDisposable disposable)
            disposable.Dispose();
        
        _isDisposed = true;
    }
} 