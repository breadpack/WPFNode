using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPFNode.Commands;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Services;
using WPFNode.Utilities;
using CommandManager = WPFNode.Commands.CommandManager;
using IWpfCommand = System.Windows.Input.ICommand;
using System.Windows.Data;
using System.Collections;
using System.ComponentModel;

namespace WPFNode.ViewModels.Nodes;

public partial class NodeCanvasViewModel : ObservableObject, INodeCanvasViewModel
{
    private readonly NodeCanvas _canvas;
    private readonly INodePluginService _pluginService;
    private readonly INodeCommandService _commandService;
    private readonly CommandManager _commandManager;

    public ObservableCollection<NodeViewModel> Nodes { get; init; }
    public ObservableCollection<ConnectionViewModel> Connections { get; init; }
    public ObservableCollection<NodeGroupViewModel> Groups { get; init; }

    [ObservableProperty]
    private double _scale = 1.0;

    [ObservableProperty]
    private double _offsetX;

    [ObservableProperty]
    private double _offsetY;

    [ObservableProperty]
    private NodeViewModel? _selectedNode;

    public IWpfCommand AddNodeCommand { get; init; }
    public IWpfCommand RemoveNodeCommand { get; init; }
    public IWpfCommand ConnectCommand { get; init; }
    public IWpfCommand DisconnectCommand { get; init; }
    public IWpfCommand AddGroupCommand { get; init; }
    public IWpfCommand RemoveGroupCommand { get; init; }
    public IWpfCommand UndoCommand { get; init; }
    public IWpfCommand RedoCommand { get; init; }
    public IWpfCommand ExecuteCommand { get; init; }
    public IWpfCommand CopyCommand { get; init; }
    public IWpfCommand PasteCommand { get; init; }
    public IWpfCommand SaveCommand { get; init; }
    public IWpfCommand LoadCommand { get; init; }

    public NodeCanvasViewModel() : this(new NodeCanvas())
    {
    }

    public NodeCanvasViewModel(NodeCanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        
        _pluginService = NodeServices.PluginService;
        _commandService = NodeServices.CommandService;
        _canvas = canvas;
        _commandManager = canvas.CommandManager;

        // 초기 컬렉션 생성
        Nodes = new ObservableCollection<NodeViewModel>();
        Connections = new ObservableCollection<ConnectionViewModel>();
        Groups = new ObservableCollection<NodeGroupViewModel>();

        // 커맨드 초기화
        AddNodeCommand = new RelayCommand<Type>(ExecuteAddNode);
        RemoveNodeCommand = new RelayCommand<NodeViewModel>(ExecuteRemoveNode);
        ConnectCommand = new RelayCommand<(NodePortViewModel, NodePortViewModel)>(ExecuteConnect);
        DisconnectCommand = new RelayCommand<(NodePortViewModel, NodePortViewModel)>(ExecuteDisconnect);
        AddGroupCommand = new RelayCommand<NodeGroup>(AddGroup);
        RemoveGroupCommand = new RelayCommand<NodeGroupViewModel>(RemoveGroup);
        UndoCommand = new RelayCommand(ExecuteUndo, CanExecuteUndo);
        RedoCommand = new RelayCommand(ExecuteRedo, CanExecuteRedo);
        ExecuteCommand = new RelayCommand(ExecuteNodes);
        CopyCommand = new RelayCommand(CopySelectedNodes);
        PasteCommand = new RelayCommand(PasteNodes);
        SaveCommand = new RelayCommand(SaveCanvas);
        LoadCommand = new RelayCommand(LoadCanvas);

        InitializeCanvas();
    }

    private void InitializeCanvas()
    {
        // 캔버스의 현재 상태로 컬렉션 초기화
        foreach (var node in _canvas.Nodes)
        {
            var nodeViewModel = CreateNodeViewModel(node);
            RegisterNodeEvents(nodeViewModel);
            Nodes.Add(nodeViewModel);
        }

        foreach (var connection in _canvas.Connections)
        {
            var connectionViewModel = new ConnectionViewModel(connection, this);
            RegisterConnectionEvents(connectionViewModel);
            Connections.Add(connectionViewModel);
        }

        RegisterCanvasEvents();
    }

    private void RegisterCanvasEvents()
    {
        // Model 변경 감지를 위한 이벤트 핸들러 등록
        _canvas.NodeAdded += OnNodeAdded;
        _canvas.NodeRemoved += OnNodeRemoved;
        _canvas.ConnectionAdded += OnConnectionAdded;
        _canvas.ConnectionRemoved += OnConnectionRemoved;
        _canvas.GroupAdded += OnGroupAdded;
        _canvas.GroupRemoved += OnGroupRemoved;
    }

    private void OnNodeAdded(object? sender, INode node)
    {
        var nodeViewModel = CreateNodeViewModel(node);
        RegisterNodeEvents(nodeViewModel);
        Nodes.Add(nodeViewModel);
    }

    private void OnNodeRemoved(object? sender, INode node)
    {
        var viewModel = Nodes.FirstOrDefault(vm => vm.Model == node);
        if (viewModel != null)
        {
            UnregisterNodeEvents(viewModel);
            Nodes.Remove(viewModel);
        }
    }

    private void OnConnectionAdded(object? sender, IConnection connection)
    {
        var connectionViewModel = new ConnectionViewModel(connection, this);
        RegisterConnectionEvents(connectionViewModel);
        Connections.Add(connectionViewModel);
    }

    private void OnConnectionRemoved(object? sender, IConnection connection)
    {
        var viewModel = Connections.FirstOrDefault(vm => vm.Model == connection);
        if (viewModel != null)
        {
            UnregisterConnectionEvents(viewModel);
            Connections.Remove(viewModel);
        }
    }

    private void OnGroupAdded(object? sender, NodeGroup group)
    {
        var groupViewModel = new NodeGroupViewModel(group, this);
        RegisterGroupEvents(groupViewModel);
        Groups.Add(groupViewModel);
    }

    private void OnGroupRemoved(object? sender, NodeGroup group)
    {
        var viewModel = Groups.FirstOrDefault(vm => vm.Model == group);
        if (viewModel != null)
        {
            UnregisterGroupEvents(viewModel);
            Groups.Remove(viewModel);
        }
    }

    /// <summary>
    /// ViewModel의 컬렉션들을 Model의 상태와 동기화합니다.
    /// </summary>
    private void SynchronizeWithModel()
    {
        // 기존 이벤트 핸들러 정리
        foreach (var node in Nodes)
        {
            UnregisterNodeEvents(node);
        }
        foreach (var connection in Connections)
        {
            UnregisterConnectionEvents(connection);
        }
        foreach (var group in Groups)
        {
            UnregisterGroupEvents(group);
        }

        using (var nodes = new DeferredCollectionChange(Nodes))
        using (var connections = new DeferredCollectionChange(Connections))
        using (var groups = new DeferredCollectionChange(Groups))
        {
            Nodes.Clear();
            foreach (var node in _canvas.Nodes)
            {
                var nodeViewModel = CreateNodeViewModel(node);
                RegisterNodeEvents(nodeViewModel);
                Nodes.Add(nodeViewModel);
            }

            Connections.Clear();
            foreach (var connection in _canvas.Connections)
            {
                var connectionViewModel = new ConnectionViewModel(connection, this);
                RegisterConnectionEvents(connectionViewModel);
                Connections.Add(connectionViewModel);
            }

            Groups.Clear();
            foreach (var group in _canvas.Groups)
            {
                var groupViewModel = new NodeGroupViewModel(group, this);
                RegisterGroupEvents(groupViewModel);
                Groups.Add(groupViewModel);
            }
        }
    }

    private void RegisterNodeEvents(NodeViewModel node)
    {
        node.PropertyChanged += OnNodePropertyChanged;
    }

    private void UnregisterNodeEvents(NodeViewModel node)
    {
        node.PropertyChanged -= OnNodePropertyChanged;
    }

    private void RegisterConnectionEvents(ConnectionViewModel connection)
    {
        connection.PropertyChanged += OnConnectionPropertyChanged;
    }

    private void UnregisterConnectionEvents(ConnectionViewModel connection)
    {
        connection.PropertyChanged -= OnConnectionPropertyChanged;
    }

    private void RegisterGroupEvents(NodeGroupViewModel group)
    {
        group.PropertyChanged += OnGroupPropertyChanged;
    }

    private void UnregisterGroupEvents(NodeGroupViewModel group)
    {
        group.PropertyChanged -= OnGroupPropertyChanged;
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is NodeViewModel viewModel && e.PropertyName == nameof(NodeViewModel.IsSelected))
        {
            if (viewModel.IsSelected)
            {
                SelectedNode = viewModel;
            }
            else if (SelectedNode == viewModel)
            {
                SelectedNode = null;
            }
        }
    }

    private void OnConnectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 필요한 경우 Connection 속성 변경 처리
    }

    private void OnGroupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 필요한 경우 Group 속성 변경 처리
    }

    private NodeViewModel CreateNodeViewModel(INode node)
    {
        if (node is not NodeBase nodeBase)
            throw new ArgumentException("노드는 NodeBase 타입이어야 합니다.");

        var viewModel = new NodeViewModel(nodeBase, _commandService, this);
        return viewModel;
    }

    private void ExecuteAddNode(Type? nodeType)
    {
        if (nodeType == null) return;

        var command = new AddNodeCommand(_canvas, nodeType);
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
        if (sourcePort == null || targetPort == null) return false;

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

        // 입력 포트에 기존 연결이 있는지 확인하고 제거
        var inputPort = source.IsInput ? source : target;
        var existingConnection = inputPort.Connections.FirstOrDefault()?.Model;
        if (existingConnection != null)
        {
            _canvas.Disconnect(existingConnection);
        }

        // 새 연결 생성
        _canvas.Connect(source.Model, target.Model);
    }

    private void ExecuteDisconnect((NodePortViewModel source, NodePortViewModel target) ports)
    {
        var connection = Connections.FirstOrDefault(c =>
            (c.Source == ports.source && c.Target == ports.target) ||
            (c.Source == ports.target && c.Target == ports.source))?.Model;

        if (connection != null)
        {
            _canvas.Disconnect(connection);
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

    private bool CanExecuteUndo() => _commandManager.CanUndo;
    private bool CanExecuteRedo() => _commandManager.CanRedo;

    private void AddGroup(NodeGroup? group)
    {
        if (group == null) return;

        var selectedNodes = Nodes
            .Where(n => n.IsSelected)
            .Select(n => (NodeBase)n.Model)
            .ToList();

        if (selectedNodes.Any())
        {
            var command = new AddGroupCommand(_canvas, group.Name, selectedNodes);
            _commandManager.Execute(command);
        }
    }

    private void RemoveGroup(NodeGroupViewModel? groupVM)
    {
        if (groupVM?.Model == null) return;

        var command = new RemoveGroupCommand(_canvas, groupVM.Model);
        _commandManager.Execute(command);
    }

    public NodePortViewModel FindPortViewModel(IPort port)
    {
        foreach (var node in Nodes)
        {
            var foundPort = node.InputPorts.FirstOrDefault(p => p.Model == port) ??
                           node.OutputPorts.FirstOrDefault(p => p.Model == port);
            if (foundPort != null)
                return foundPort;
        }
        return null;
    }

    public void OnPortsChanged()
    {
        // SynchronizeWithModel();
    }

    public NodeCanvas Model => _canvas;

    private async void ExecuteNodes()
    {
        try
        {
            await _canvas.ExecuteAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"노드 실행 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopySelectedNodes()
    {
        var selectedNodes = Nodes.Where(n => n.IsSelected).ToList();
        if (selectedNodes.Any())
        {
            var nodeDataList = selectedNodes.Select(n => ((NodeBase)n.Model).CreateCopy()).ToList();
            System.Windows.Clipboard.SetDataObject(nodeDataList);
        }
    }

    private void PasteNodes()
    {
        var dataObject = System.Windows.Clipboard.GetDataObject();
        if (dataObject?.GetData(typeof(List<NodeBase>)) is List<NodeBase> nodeDataList)
        {
            foreach (var node in nodeDataList)
            {
                var newNode = _canvas.CreateNode(node.GetType(), node.X + 20, node.Y + 20);
                // 필요한 속성들을 복사
                if (newNode is NodeBase nodeBase)
                {
                    nodeBase.Name = node.Name;
                }
            }
            // SynchronizeWithModel();
        }
    }

    private async void SaveCanvas()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Node Canvas Files (*.nodecanvas)|*.nodecanvas|All Files (*.*)|*.*",
            DefaultExt = ".nodecanvas"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _canvas.SaveToFileAsync(dialog.FileName);
                MessageBox.Show("캔버스가 저장되었습니다.", "저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 중 오류가 발생했습니다: {ex.Message}", "저장 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void LoadCanvas()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Node Canvas Files (*.nodecanvas)|*.nodecanvas|All Files (*.*)|*.*",
            DefaultExt = ".nodecanvas"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _canvas.LoadFromFileAsync(dialog.FileName);
                SynchronizeWithModel();
                MessageBox.Show("캔버스를 불러왔습니다.", "불러오기 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"불러오기 중 오류가 발생했습니다: {ex.Message}", "불러오기 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}