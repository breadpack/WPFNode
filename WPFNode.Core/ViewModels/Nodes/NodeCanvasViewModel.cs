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
            canvas.Nodes.Select(n => new NodeViewModel(n, commandService)));
        
        _connections = new ObservableCollection<ConnectionViewModel>(
            canvas.Connections.Select(c => new ConnectionViewModel(c)));
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
            if (e.NewItems != null)
            {
                foreach (NodeBase node in e.NewItems)
                {
                    _nodes.Add(new NodeViewModel(node, commandService));
                }
            }
            if (e.OldItems != null)
            {
                foreach (NodeBase node in e.OldItems)
                {
                    var vm = _nodes.FirstOrDefault(n => n.Model == node);
                    if (vm != null)
                        _nodes.Remove(vm);
                }
            }
        };

        _canvas.Connections.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (Connection conn in e.NewItems)
                {
                    _connections.Add(new ConnectionViewModel(conn));
                }
            }
            if (e.OldItems != null)
            {
                foreach (Connection conn in e.OldItems)
                {
                    var vm = _connections.FirstOrDefault(c => c.Model == conn);
                    if (vm != null)
                        _connections.Remove(vm);
                }
            }
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

    private void ExecuteConnect((NodePortViewModel, NodePortViewModel) ports)
    {
        var (sourcePort, targetPort) = ports;
        if (sourcePort == null || targetPort == null) return;

        var command = new ConnectCommand(_canvas, sourcePort.Model, targetPort.Model);
        _commandManager.Execute(command);
    }

    private void ExecuteDisconnect((NodePortViewModel, NodePortViewModel) ports)
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

    public NodeCanvas Model => _canvas;
} 