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
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

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
    
    // 선택 가능한 모든 항목을 저장하는 컬렉션
    public ObservableCollection<ISelectable> SelectableItems { get; init; }
    
    // 선택된 항목들을 저장하는 컬렉션
    [ObservableProperty]
    private ObservableCollection<ISelectable> _selectedItems;

    [ObservableProperty]
    private double _scale = 1.0;

    [ObservableProperty]
    private double _offsetX;

    [ObservableProperty]
    private double _offsetY;

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
        SelectableItems = new ObservableCollection<ISelectable>();
        _selectedItems = new ObservableCollection<ISelectable>();

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
        SynchronizeWithModel();
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

    private void UnregisterCanvasEvents()
    {
        // Model 변경 감지를 위한 이벤트 핸들러 해제
        _canvas.NodeAdded -= OnNodeAdded;
        _canvas.NodeRemoved -= OnNodeRemoved;
        _canvas.ConnectionAdded -= OnConnectionAdded;
        _canvas.ConnectionRemoved -= OnConnectionRemoved;
        _canvas.GroupAdded -= OnGroupAdded;
        _canvas.GroupRemoved -= OnGroupRemoved;
    }

    /// <summary>
    /// 캔버스의 모든 내용을 제거합니다.
    /// </summary>
    private void ClearCanvas()
    {
        foreach (var connection in _canvas.Connections.ToList())
        {
            _canvas.Disconnect(connection);
        }
        foreach (var node in _canvas.Nodes.ToList())
        {
            _canvas.RemoveNode(node);
        }
        foreach (var group in _canvas.Groups.ToList())
        {
            _canvas.RemoveGroup(group);
        }
    }

    /// <summary>
    /// 소스 캔버스의 내용을 현재 캔버스로 복사합니다.
    /// </summary>
    private void CopyFromCanvas(NodeCanvas sourceCanvas)
    {
        // 새 캔버스의 내용을 복사
        foreach (var node in sourceCanvas.Nodes)
        {
            var newNode = _canvas.CreateNodeWithGuid(node.Guid, node.GetType(), node.X, node.Y);
            if (newNode is NodeBase newNodeBase && node is NodeBase sourceNodeBase)
            {
                newNodeBase.Name = sourceNodeBase.Name;
                newNodeBase.Description = sourceNodeBase.Description;
            }
        }
        foreach (var connection in sourceCanvas.Connections)
        {
            var sourcePort = _canvas.Nodes
                .SelectMany(n => n.OutputPorts)
                .FirstOrDefault(p => p.Id == connection.SourcePortId);
            var targetPort = _canvas.Nodes
                .SelectMany(n => n.InputPorts)
                .FirstOrDefault(p => p.Id == connection.TargetPortId);
            
            if (sourcePort != null && targetPort != null)
            {
                Console.WriteLine($"연결 복원 시도: {connection.Guid}");
                _canvas.ConnectWithId(connection.Guid, sourcePort, targetPort);
                Console.WriteLine($"연결 복원 성공: {connection.Guid}");
            }
        }
        foreach (var group in sourceCanvas.Groups)
        {
            var nodes = group.Nodes
                .Select(n => _canvas.Nodes.FirstOrDefault(cn => cn.Guid == n.Guid))
                .Where(n => n != null)
                .Cast<NodeBase>()
                .ToList();
            if (nodes.Any())
            {
                _canvas.CreateGroupWithId(group.Id, nodes, group.Name);
            }
        }
    }

    private void OnNodeAdded(object? sender, INode node)
    {
        var nodeViewModel = CreateNodeViewModel(node);
        RegisterNodeEvents(nodeViewModel);
        Nodes.Add(nodeViewModel);
        SelectableItems.Add(nodeViewModel);
    }

    private void OnNodeRemoved(object? sender, INode node)
    {
        var viewModel = Nodes.FirstOrDefault(vm => vm.Model == node);
        if (viewModel != null)
        {
            UnregisterNodeEvents(viewModel);
            Nodes.Remove(viewModel);
            SelectableItems.Remove(viewModel);
            viewModel.Dispose();
        }
    }

    private void OnConnectionAdded(object? sender, IConnection connection)
    {
        var connectionViewModel = new ConnectionViewModel(connection, this);
        RegisterConnectionEvents(connectionViewModel);
        Connections.Add(connectionViewModel);
        SelectableItems.Add(connectionViewModel);
    }

    private void OnConnectionRemoved(object? sender, IConnection connection)
    {
        var viewModel = Connections.FirstOrDefault(vm => vm.Model == connection);
        if (viewModel != null)
        {
            UnregisterConnectionEvents(viewModel);
            Connections.Remove(viewModel);
            SelectableItems.Remove(viewModel);
            viewModel.Dispose();
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
            viewModel.Dispose();
        }
    }

    /// <summary>
    /// ViewModel의 컬렉션들을 Model의 상태와 동기화합니다.
    /// </summary>
    private void SynchronizeWithModel()
    {
        // 컬렉션 동기화 일시 중지
        BindingOperations.DisableCollectionSynchronization(Nodes);
        BindingOperations.DisableCollectionSynchronization(Connections);
        BindingOperations.DisableCollectionSynchronization(Groups);
        BindingOperations.DisableCollectionSynchronization(SelectableItems);

        foreach (var node in Nodes) {
            node.Dispose();
        }
        Nodes.Clear();
        
        foreach (var node in _canvas.Nodes)
        {
            var nodeViewModel = CreateNodeViewModel(node);
            RegisterNodeEvents(nodeViewModel);
            Nodes.Add(nodeViewModel);
            SelectableItems.Add(nodeViewModel);
        }

        foreach (var connection in Connections) {
            connection.Dispose();
        }
        Connections.Clear();
        foreach (var connection in _canvas.Connections)
        {
            var connectionViewModel = new ConnectionViewModel(connection, this);
            RegisterConnectionEvents(connectionViewModel);
            Connections.Add(connectionViewModel);
            SelectableItems.Add(connectionViewModel);
        }
        
        foreach (var group in Groups) {
            group.Dispose();
        }
        Groups.Clear();
        foreach (var group in _canvas.Groups)
        {
            var groupViewModel = new NodeGroupViewModel(group, this);
            RegisterGroupEvents(groupViewModel);
            Groups.Add(groupViewModel);
            // 추후 그룹도 ISelectable 구현하면 추가
            // SelectableItems.Add(groupViewModel);
        }

        // 컬렉션 동기화 재개
        BindingOperations.EnableCollectionSynchronization(Nodes, new object());
        BindingOperations.EnableCollectionSynchronization(Connections, new object());
        BindingOperations.EnableCollectionSynchronization(Groups, new object());
        BindingOperations.EnableCollectionSynchronization(SelectableItems, new object());
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
        }
    }

    private void OnConnectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is ConnectionViewModel viewModel && e.PropertyName == nameof(ConnectionViewModel.IsSelected))
        {
        }
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
        var sourcePort = _canvas.Nodes
            .SelectMany(n => n.InputPorts.Cast<IPort>().Concat(n.OutputPorts.Cast<IPort>()))
            .FirstOrDefault(p => p.Id == ports.source.Id);
        var targetPort = _canvas.Nodes
            .SelectMany(n => n.InputPorts.Cast<IPort>().Concat(n.OutputPorts.Cast<IPort>()))
            .FirstOrDefault(p => p.Id == ports.target.Id);
        
        if (sourcePort != null && targetPort != null)
        {
            Console.WriteLine($"연결 복원 시도: {ports.source.Id}");
            _canvas.Connect(sourcePort, targetPort);
            Console.WriteLine($"연결 복원 성공: {ports.source.Id}");
        }
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

        if (selectedNodes.Count != 0)
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

    public NodeViewModel? FindNodeById(Guid nodeId)
    {
        return Nodes.FirstOrDefault(n => n.Model.Guid == nodeId);
    }

    public IEnumerable<NodeViewModel> FindNodesByName(string name) {
        return string.IsNullOrWhiteSpace(name) ? [] : Nodes.Where(n => n.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<NodeViewModel> FindNodesByType(Type nodeType)
    {
        return Nodes.Where(n => nodeType.IsInstanceOfType(n.Model));
    }

    public async void ExecuteNodes()
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
                await SaveAsync(dialog.FileName);
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
                await LoadAsync(dialog.FileName);
                MessageBox.Show("캔버스를 불러왔습니다.", "불러오기 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"불러오기 중 오류가 발생했습니다: {ex.Message}", "불러오기 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public async Task SaveAsync(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        await _canvas.SaveToFileAsync(filePath);
    }

    public async Task LoadAsync(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        await _canvas.LoadFromFileAsync(filePath);
        SynchronizeWithModel();
    }

    public async Task<string> ToJsonAsync()
    {
        return await Task.Run(() => _canvas.ToJson());
    }

    public async Task LoadFromJsonAsync(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        
        // JSON 파싱은 백그라운드 스레드에서 수행
        var newCanvas = await Task.Run(() => NodeCanvas.FromJson(json));
        
        // UI 관련 작업은 메인 스레드에서 수행
        var context = SynchronizationContext.Current;
        if (context != null)
        {
            var tcs = new TaskCompletionSource();
            context.Post(_ =>
            {
                try
                {
                    // 컬렉션 동기화 일시 중지
                    BindingOperations.DisableCollectionSynchronization(Nodes);
                    BindingOperations.DisableCollectionSynchronization(Connections);
                    BindingOperations.DisableCollectionSynchronization(Groups);
                    BindingOperations.DisableCollectionSynchronization(SelectableItems);

                    UnregisterCanvasEvents();
                    ClearCanvas();
                    
                    // 이벤트 핸들러 등록
                    _canvas.NodeAdded += OnNodeAdded;
                    _canvas.NodeRemoved += OnNodeRemoved;
                    _canvas.ConnectionAdded += OnConnectionAdded;
                    _canvas.ConnectionRemoved += OnConnectionRemoved;
                    _canvas.GroupAdded += OnGroupAdded;
                    _canvas.GroupRemoved += OnGroupRemoved;
                    
                    CopyFromCanvas(newCanvas);
                    SynchronizeWithModel();

                    // 컬렉션 동기화 재개
                    BindingOperations.EnableCollectionSynchronization(Nodes, new object());
                    BindingOperations.EnableCollectionSynchronization(Connections, new object());
                    BindingOperations.EnableCollectionSynchronization(Groups, new object());
                    BindingOperations.EnableCollectionSynchronization(SelectableItems, new object());

                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);
            await tcs.Task;
        }
        else
        {
            UnregisterCanvasEvents();
            ClearCanvas();
            CopyFromCanvas(newCanvas);
            RegisterCanvasEvents();
            SynchronizeWithModel();
        }
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

    /// <summary>
    /// 항목이 선택되었는지 확인합니다.
    /// </summary>
    /// <param name="item">확인할 항목</param>
    /// <returns>항목이 선택되었으면 true, 아니면 false</returns>
    public bool IsItemSelected(ISelectable item)
    {
        return SelectedItems.Contains(item);
    }

    /// <summary>
    /// 항목을 선택합니다.
    /// </summary>
    /// <param name="selectable">선택할 항목</param>
    /// <param name="clearOthers">다른 항목의 선택을 해제할지 여부</param>
    public void SelectItem(ISelectable selectable, bool clearOthers = true)
    {
        if (clearOthers)
        {
            ClearSelection();
        }
        
        if (!SelectedItems.Contains(selectable))
        {
            SelectedItems.Add(selectable);
            
            // 선택 변경 이벤트 발생
            OnPropertyChanged(nameof(SelectedItems));
        }
    }

    public void SelectAll() {
        ClearSelection();
        foreach (var item in SelectableItems) {
            SelectedItems.Add(item);
        }
        OnPropertyChanged(nameof(SelectedItems));
    }

    /// <summary>
    /// 항목의 선택을 해제합니다.
    /// </summary>
    /// <param name="selectable">선택 해제할 항목</param>
    public void DeselectItem(ISelectable selectable)
    {
        if (SelectedItems.Contains(selectable))
        {
            SelectedItems.Remove(selectable);
            // 선택 변경 이벤트 발생
            OnPropertyChanged(nameof(SelectedItems));
        }
    }

    /// <summary>
    /// 모든 선택 가능한 항목의 선택을 해제합니다.
    /// </summary>
    public void ClearSelection()
    {
        SelectedItems.Clear();
        
        // 선택 변경 이벤트 발생
        OnPropertyChanged(nameof(SelectedItems));
    }
    
    /// <summary>
    /// 선택된 모든 항목을 반환합니다.
    /// </summary>
    public IEnumerable<ISelectable> GetSelectedItems()
    {
        return SelectedItems;
    }
    
    /// <summary>
    /// 지정된 유형의 선택된 항목을 반환합니다.
    /// </summary>
    public IEnumerable<T> GetSelectedItemsOfType<T>() where T : ISelectable
    {
        return SelectedItems.OfType<T>();
    }

    /// <summary>
    /// ID로 항목을 찾아 반환합니다.
    /// </summary>
    public ISelectable? FindSelectableById(Guid id)
    {
        return SelectableItems.FirstOrDefault(item => item.Id == id);
    }
}