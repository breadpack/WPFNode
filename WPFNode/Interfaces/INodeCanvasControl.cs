using System;
using WPFNode.ViewModels.Nodes;

namespace WPFNode.Interfaces;

/// <summary>
/// 노드 캔버스 컨트롤 인터페이스
/// </summary>
public interface INodeCanvasControl
{
    INodeCanvasViewModel? ViewModel { get; }
    INodeModelService PluginService { get; }
    INodeCommandService CommandService { get; }
    
    event EventHandler<NodeViewModel>? NodeAdded;
    event EventHandler<NodeViewModel>? NodeRemoved;
    event EventHandler<ConnectionViewModel>? ConnectionAdded;
    event EventHandler<ConnectionViewModel>? ConnectionRemoved;
    event EventHandler<NodeViewModel>? NodeMoved;
    event EventHandler<NodeViewModel>? NodeSelected;
    event EventHandler<NodeViewModel>? NodeDeselected;
} 