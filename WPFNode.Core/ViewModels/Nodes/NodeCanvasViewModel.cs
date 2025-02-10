using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using WPFNode.Core.Models;
using WPFNode.Core.ViewModels.Base;
using WPFNode.Core.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using WPFNode.Core.Interfaces;
using WPFNode.Abstractions;
using WPFNode.Core.Commands;
using IWpfCommand = System.Windows.Input.ICommand;
using WPFNode.Plugin.SDK;
using System.Collections.Specialized;

namespace WPFNode.Core.ViewModels.Nodes;

public partial class NodeCanvasViewModel : ObservableObject
{
    private readonly NodeCanvas _canvas;
    private readonly ObservableCollection<NodeViewModel> _nodes;
    private readonly ObservableCollection<ConnectionViewModel> _connections;
    private readonly ObservableCollection<NodeGroupViewModel> _groups;
    private readonly INodePluginService _pluginService;
    private readonly INodeCommandService _commandService;
    private readonly WPFNode.Core.Commands.CommandManager _commandManager;

    public ObservableCollection<NodeViewModel> Nodes => _nodes;
    public IReadOnlyList<ConnectionViewModel> Connections => _connections;
    public IReadOnlyList<NodeGroupViewModel> Groups => _groups;

    [ObservableProperty]
    private double _scale = 1.0;

    [ObservableProperty]
    private double _offsetX;

    [ObservableProperty]
    private double _offsetY;

    public IWpfCommand AddNodeCommand { get; }
    public IWpfCommand RemoveNodeCommand { get; }
    public IWpfCommand ConnectCommand { get; }
    public IWpfCommand DisconnectCommand { get; }
    public IWpfCommand AddGroupCommand { get; }
    public IWpfCommand RemoveGroupCommand { get; }
    public IWpfCommand UndoCommand { get; }
    public IWpfCommand RedoCommand { get; }

    public NodeCanvasViewModel(
        NodeCanvas canvas,
        INodePluginService pluginService,
        INodeCommandService commandService)
    {
        _canvas = canvas;
        _pluginService = pluginService;
        _commandService = commandService;
        _commandManager = new WPFNode.Core.Commands.CommandManager();
        
        _nodes = new ObservableCollection<NodeViewModel>(
            canvas.Nodes.Select(n => new NodeViewModel(n, commandService, this)));
        
        _connections = new ObservableCollection<ConnectionViewModel>(
            canvas.Connections.Select(c => new ConnectionViewModel(c, this)));
        _groups = new ObservableCollection<NodeGroupViewModel>();

        AddNodeCommand = new RelayCommand<Type>(ExecuteAddNode);
        RemoveNodeCommand = new RelayCommand<NodeViewModel>(ExecuteRemoveNode);
        ConnectCommand = new RelayCommand<(NodePortViewModel, NodePortViewModel)>(ExecuteConnect);
        DisconnectCommand = new RelayCommand<(NodePortViewModel, NodePortViewModel)>(ExecuteDisconnect);
        AddGroupCommand = new RelayCommand<NodeGroup>(AddGroup);
        RemoveGroupCommand = new RelayCommand<NodeGroupViewModel>(RemoveGroup);
        UndoCommand = new RelayCommand(ExecuteUndo, CanExecuteUndo);
        RedoCommand = new RelayCommand(ExecuteRedo, CanExecuteRedo);

        // Model 변경 감지
        _canvas.Nodes.CollectionChanged += (s, e) =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (NodeBase node in e.NewItems)
                        {
                            _nodes.Add(new NodeViewModel(node, commandService, this));
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (NodeBase node in e.OldItems)
                        {
                            var vm = _nodes.FirstOrDefault(n => n.Model == node);
                            if (vm != null)
                            {
                                _nodes.Remove(vm);
                            }
                        }
                        ValidateConnections();
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null && e.NewItems != null)
                    {
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            var oldNode = (NodeBase)e.OldItems[i]!;
                            var newNode = (NodeBase)e.NewItems[i]!;
                            var oldVm = _nodes.FirstOrDefault(n => n.Model == oldNode);
                            if (oldVm != null)
                            {
                                var index = _nodes.IndexOf(oldVm);
                                _nodes[index] = new NodeViewModel(newNode, commandService, this);
                            }
                        }
                        ValidateConnections();
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _nodes.Clear();
                    foreach (var node in _canvas.Nodes)
                    {
                        _nodes.Add(new NodeViewModel(node, commandService, this));
                    }
                    ValidateConnections();
                    break;
            }
        };

        _canvas.Connections.CollectionChanged += (s, e) =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (Connection conn in e.NewItems)
                        {
                            _connections.Add(new ConnectionViewModel(conn, this));
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (Connection conn in e.OldItems)
                        {
                            var vm = _connections.FirstOrDefault(c => c.Model == conn);
                            if (vm != null)
                            {
                                _connections.Remove(vm);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null && e.NewItems != null)
                    {
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            var oldConn = (Connection)e.OldItems[i]!;
                            var newConn = (Connection)e.NewItems[i]!;
                            var oldVm = _connections.FirstOrDefault(c => c.Model == oldConn);
                            if (oldVm != null)
                            {
                                var index = _connections.IndexOf(oldVm);
                                _connections[index] = new ConnectionViewModel(newConn, this);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _connections.Clear();
                    foreach (var conn in _canvas.Connections)
                    {
                        _connections.Add(new ConnectionViewModel(conn, this));
                    }
                    break;
            }

            // 연결이 변경될 때마다 유효성 검사 수행
            ValidateConnections();
        };
    }

    private void ExecuteAddNode(Type? nodeType)
    {
        if (nodeType == null) return;
        var node = (NodeBase)_pluginService.CreateNode(nodeType);
        var command = new AddNodeCommand(_canvas, node);
        _commandManager.Execute(command);
    }

    private void ExecuteRemoveNode(NodeViewModel? nodeViewModel)
    {
        if (nodeViewModel == null) return;
        var command = new RemoveNodeCommand(_canvas, nodeViewModel.Model);
        _commandManager.Execute(command);
    }

    private bool IsValidConnection(NodePortViewModel sourcePort, NodePortViewModel targetPort)
    {
        // 같은 노드의 포트인 경우 연결 불가
        var sourceNode = Nodes.FirstOrDefault(n => 
            n.InputPorts.Contains(sourcePort) || n.OutputPorts.Contains(sourcePort));
        var targetNode = Nodes.FirstOrDefault(n => 
            n.InputPorts.Contains(targetPort) || n.OutputPorts.Contains(targetPort));
            
        if (sourceNode == null || targetNode == null || sourceNode == targetNode)
            return false;

        // 입력-출력 포트 방향이 맞는지 확인
        if (sourcePort.IsInput == targetPort.IsInput)
            return false;

        // 이미 연결된 포트인지 확인
        if (Connections.Any(c => 
            (c.Source == sourcePort && c.Target == targetPort) ||
            (c.Source == targetPort && c.Target == sourcePort)))
            return false;

        // 포트 타입 호환성 확인
        return sourcePort.CanConnectTo(targetPort);
    }

    private void ExecuteConnect((NodePortViewModel source, NodePortViewModel target) ports)
    {
        if (!IsValidConnection(ports.source, ports.target))
            return;

        var (source, target) = ports;
        
        // 입력 포트에 기존 연결이 있는지 확인
        var inputPort = source.IsInput ? source : target;
        var existingConnection = inputPort.Connections.FirstOrDefault()?.Model;

        if (existingConnection != null)
        {
            var replaceCommand = new ReplaceConnectionCommand(_canvas, source.Model, target.Model, existingConnection);
            _commandManager.Execute(replaceCommand);
        }
        else
        {
            var connectCommand = new ConnectCommand(_canvas, source.Model, target.Model);
            _commandManager.Execute(connectCommand);
        }
    }

    private void ExecuteDisconnect((NodePortViewModel source, NodePortViewModel target) ports)
    {
        var (sourcePort, targetPort) = ports;
        if (sourcePort == null || targetPort == null) return;

        var connection = sourcePort.Connections
            .FirstOrDefault(c => c.Target.Id == targetPort.Id || c.Source.Id == targetPort.Id)?.Model;
        
        if (connection != null)
        {
            var command = new DisconnectCommand(_canvas, connection);
            _commandManager.Execute(command);
        }
    }

    private void ExecuteUndo()
    {
        _commandManager.Undo();
    }

    private void ExecuteRedo()
    {
        _commandManager.Redo();
    }

    private bool CanExecuteUndo()
    {
        return _commandManager.CanUndo;
    }

    private bool CanExecuteRedo()
    {
        return _commandManager.CanRedo;
    }

    private void AddGroup(NodeGroup? group)
    {
        if (group == null) return;
        var command = new AddGroupCommand(_canvas, group.Name, group.Nodes);
        _commandManager.Execute(command);
    }

    private void RemoveGroup(NodeGroupViewModel? groupVM)
    {
        if (groupVM == null) return;
        var command = new RemoveGroupCommand(_canvas, groupVM.Model);
        _commandManager.Execute(command);
    }

    public IEnumerable<Type> SearchNodes(string searchText)
    {
        searchText = searchText.Trim().ToLower();
        return _pluginService.NodeTypes
            .Where(t => 
            {
                var node = _pluginService.CreateNode(t);
                return node.Name.ToLower().Contains(searchText) ||
                       node.Category.ToLower().Contains(searchText) ||
                       node.Description.ToLower().Contains(searchText);
            })
            .ToList();
    }

    public NodePortViewModel? FindPortViewModel(IPort port)
    {
        foreach (var node in _nodes)
        {
            var portVM = node.InputPorts.FirstOrDefault(p => p.Model == port) ??
                        node.OutputPorts.FirstOrDefault(p => p.Model == port);
            if (portVM != null)
                return portVM;
        }
        return null;
    }

    public NodeCanvas Model => _canvas;

    private void ValidateConnections()
    {
        var invalidConnections = _canvas.Connections
            .Where(c => !IsValidPort(c.Source) || !IsValidPort(c.Target))
            .ToList();

        foreach (var connection in invalidConnections)
        {
            var command = new DisconnectCommand(_canvas, connection);
            _commandManager.Execute(command);
        }
    }

    private bool IsValidPort(IPort port)
    {
        return _nodes.Any(n => 
            n.InputPorts.Any(p => p.Model == port) || 
            n.OutputPorts.Any(p => p.Model == port));
    }
} 