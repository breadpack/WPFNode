using System.Collections.ObjectModel;
using System.Windows.Input;
using WPFNode.Core.Models;
using WPFNode.Core.ViewModels.Base;
using CommunityToolkit.Mvvm.Input;
using WPFNode.Core.Services;

namespace WPFNode.Core.ViewModels.Nodes;

public class NodeCanvasViewModel : ViewModelBase
{
    private readonly NodeCanvas _canvas;
    private readonly ObservableCollection<NodeViewModel> _nodes;
    private readonly ObservableCollection<ConnectionViewModel> _connections;
    private readonly ObservableCollection<NodeGroupViewModel> _groups;
    private readonly NodeTemplateService _templateService;
    private double _scale = 1.0;
    private double _offsetX;
    private double _offsetY;

    public NodeCanvasViewModel(NodeCanvas canvas, NodeTemplateService templateService)
    {
        _canvas = canvas;
        _templateService = templateService;
        _nodes = new ObservableCollection<NodeViewModel>(
            canvas.Nodes.Select(n => new NodeViewModel(n)));
        _connections = new ObservableCollection<ConnectionViewModel>(
            canvas.Connections.Select(c => new ConnectionViewModel(c)));
        _groups = new ObservableCollection<NodeGroupViewModel>();

        // 커맨드 초기화
        AddNodeCommand = new RelayCommand<Node>(AddNode);
        RemoveNodeCommand = new RelayCommand<NodeViewModel>(RemoveNode);
        ConnectCommand = new RelayCommand<(NodePortViewModel source, NodePortViewModel target)>(Connect);
        DisconnectCommand = new RelayCommand<ConnectionViewModel>(Disconnect);
        AddGroupCommand = new RelayCommand<NodeGroup>(AddGroup);
        RemoveGroupCommand = new RelayCommand<NodeGroupViewModel>(RemoveGroup);
        UndoCommand = new RelayCommand(Undo);
        RedoCommand = new RelayCommand(Redo);

        // Model 변경 감지
        _canvas.Nodes.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (Node node in e.NewItems)
                {
                    _nodes.Add(new NodeViewModel(node));
                }
            }
            if (e.OldItems != null)
            {
                foreach (Node node in e.OldItems)
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

    public IReadOnlyList<NodeViewModel> Nodes => _nodes;
    public IReadOnlyList<ConnectionViewModel> Connections => _connections;
    public IReadOnlyList<NodeGroupViewModel> Groups => _groups;

    public double Scale
    {
        get => _scale;
        set
        {
            if (SetProperty(ref _scale, value))
            {
                _canvas.Scale = value;
            }
        }
    }

    public double OffsetX
    {
        get => _offsetX;
        set
        {
            if (SetProperty(ref _offsetX, value))
            {
                _canvas.OffsetX = value;
            }
        }
    }

    public double OffsetY
    {
        get => _offsetY;
        set
        {
            if (SetProperty(ref _offsetY, value))
            {
                _canvas.OffsetY = value;
            }
        }
    }

    public ICommand AddNodeCommand { get; }
    public ICommand RemoveNodeCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand AddGroupCommand { get; }
    public ICommand RemoveGroupCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }

    private void AddNode(Node? node)
    {
        if (node == null) return;
        _canvas.Nodes.Add(node);
    }

    private void RemoveNode(NodeViewModel? nodeVM)
    {
        if (nodeVM == null) return;
        _canvas.Nodes.Remove(nodeVM.Model);
    }

    private void Connect((NodePortViewModel source, NodePortViewModel target) ports)
    {
        if (ports.source == null || ports.target == null) return;
        var connection = new Connection(Guid.NewGuid().ToString(), ports.source.Model, ports.target.Model);
        _canvas.Connections.Add(connection);
    }

    private void Disconnect(ConnectionViewModel? connectionVM)
    {
        if (connectionVM == null) return;
        connectionVM.Model.Disconnect();
        _canvas.Connections.Remove(connectionVM.Model);
    }

    private void AddGroup(NodeGroup? group)
    {
        if (group == null) return;
        _groups.Add(new NodeGroupViewModel(group));
    }

    private void RemoveGroup(NodeGroupViewModel? groupVM)
    {
        if (groupVM == null) return;
        _groups.Remove(groupVM);
    }

    private void Undo()
    {
        _canvas.CommandManager.Undo();
    }

    private void Redo()
    {
        _canvas.CommandManager.Redo();
    }

    public IEnumerable<NodeTemplate> SearchNodes(string searchText)
    {
        searchText = searchText.Trim().ToLower();
        return _templateService.Templates
            .Where(t => t.Name.ToLower().Contains(searchText) ||
                       t.Category.ToLower().Contains(searchText) ||
                       t.Description.ToLower().Contains(searchText))
            .ToList();
    }

    public NodeCanvas Model => _canvas;
} 