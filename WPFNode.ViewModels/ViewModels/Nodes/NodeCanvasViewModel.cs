using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Services;
using IWpfCommand = System.Windows.Input.ICommand;
using System.Windows.Data;
using System.ComponentModel;
using WPFNode.Commands;
using WPFNode.Utilities;

namespace WPFNode.ViewModels.Nodes;

public partial class NodeCanvasViewModel : ObservableObject, INodeCanvasViewModel {
    private readonly NodeCanvas          _canvas;
    private          INodeCommandService _commandService;

    public ObservableCollection<NodeViewModel>       Nodes       { get; }
    public ObservableCollection<ConnectionViewModel> Connections { get; }
    public ObservableCollection<NodeGroupViewModel>  Groups      { get; }

    // 선택 가능한 모든 항목을 저장하는 컬렉션
    public ObservableCollection<ISelectable> SelectableItems { get; }

    // 선택된 항목들을 저장하는 컬렉션
    [ObservableProperty]
    private ObservableCollection<ISelectable> _selectedItems;

    [ObservableProperty]
    private double _scale = 1.0;

    [ObservableProperty]
    private double _offsetX;

    [ObservableProperty]
    private double _offsetY;

    public IWpfCommand AddNodeCommand     { get; init; }
    public IWpfCommand AddNodeAtCommand   { get; init; }
    public IWpfCommand RemoveNodeCommand  { get; init; }
    public IWpfCommand ConnectCommand     { get; init; }
    public IWpfCommand DisconnectCommand  { get; init; }
    public IWpfCommand AddGroupCommand    { get; init; }
    public IWpfCommand RemoveGroupCommand { get; init; }
    public IWpfCommand UndoCommand        { get; init; }
    public IWpfCommand RedoCommand        { get; init; }
    public IWpfCommand ExecuteCommand     { get; init; }
    public IWpfCommand CopyCommand        { get; init; }
    public IWpfCommand PasteCommand       { get; init; }
    public IWpfCommand DuplicateCommand   { get; init; }
    public IWpfCommand SaveCommand        { get; init; }
    public IWpfCommand LoadCommand        { get; init; }

    // 클립보드 형식 상수 정의
    private const string ClipboardFormat = "WPFNode.NodeData";

    public NodeCanvasViewModel(NodeCanvas canvas) {
        ArgumentNullException.ThrowIfNull(canvas);

        _commandService = NodeServices.CommandService;
        _canvas         = canvas;

        // 초기 컬렉션 생성
        Nodes           = new();
        Connections     = new();
        Groups          = new();
        SelectableItems = new();
        _selectedItems  = new();

        // 커맨드 초기화
        AddNodeCommand     = new RelayCommand<Type>(ExecuteAddNode);
        AddNodeAtCommand   = new RelayCommand<(Type nodeType, double x, double y)>(ExecuteAddNodeAt);
        RemoveNodeCommand  = new RelayCommand<NodeViewModel>(ExecuteRemoveNode);
        ConnectCommand     = new RelayCommand<(NodePortViewModel, NodePortViewModel)>(ExecuteConnect);
        DisconnectCommand  = new RelayCommand<(NodePortViewModel, NodePortViewModel)>(ExecuteDisconnect);
        AddGroupCommand    = new RelayCommand<NodeGroup>(AddGroup);
        RemoveGroupCommand = new RelayCommand<NodeGroupViewModel>(RemoveGroup);
        UndoCommand        = new RelayCommand(ExecuteUndo, CanExecuteUndo);
        RedoCommand        = new RelayCommand(ExecuteRedo, CanExecuteRedo);
        ExecuteCommand     = new RelayCommand(ExecuteNodes);
        CopyCommand        = new RelayCommand(CopySelectedNodes);
        PasteCommand       = new RelayCommand(PasteNodes);
        DuplicateCommand   = new RelayCommand(DuplicateSelectedNodes);
        SaveCommand        = new RelayCommand(SaveCanvas);
        LoadCommand        = new RelayCommand(LoadCanvas);

        InitializeCanvas();
    }

    private void InitializeCanvas() {
        // 캔버스의 현재 상태로 컬렉션 초기화
        SynchronizeWithModel();
        RegisterCanvasEvents();
    }

    private void RegisterCanvasEvents() {
        // Model 변경 감지를 위한 이벤트 핸들러 등록
        _canvas.NodeAdded         += OnNodeAdded;
        _canvas.NodeRemoved       += OnNodeRemoved;
        _canvas.ConnectionAdded   += OnConnectionAdded;
        _canvas.ConnectionRemoved += OnConnectionRemoved;
        _canvas.GroupAdded        += OnGroupAdded;
        _canvas.GroupRemoved      += OnGroupRemoved;
    }

    private void UnregisterCanvasEvents() {
        // Model 변경 감지를 위한 이벤트 핸들러 해제
        _canvas.NodeAdded         -= OnNodeAdded;
        _canvas.NodeRemoved       -= OnNodeRemoved;
        _canvas.ConnectionAdded   -= OnConnectionAdded;
        _canvas.ConnectionRemoved -= OnConnectionRemoved;
        _canvas.GroupAdded        -= OnGroupAdded;
        _canvas.GroupRemoved      -= OnGroupRemoved;
    }

    /// <summary>
    /// 캔버스의 모든 내용을 제거합니다.
    /// </summary>
    private void ClearCanvas() {
        foreach (var connection in _canvas.Connections.ToList()) {
            _canvas.Disconnect(connection);
        }

        foreach (var node in _canvas.Nodes.ToList()) {
            _canvas.RemoveNode(node);
        }

        foreach (var group in _canvas.Groups.ToList()) {
            _canvas.RemoveGroup(group);
        }
    }

    /// <summary>
    /// 소스 캔버스의 내용을 현재 캔버스로 복사합니다.
    /// </summary>
    private void CopyFromCanvas(NodeCanvas sourceCanvas) {
        // 새 캔버스의 내용을 복사
        foreach (var node in sourceCanvas.Nodes) {
            var newNode = _canvas.CreateNodeWithGuid(node.Guid, node.GetType(), node.X, node.Y);
            if (newNode is NodeBase newNodeBase && node is NodeBase sourceNodeBase) {
                newNodeBase.Name        = sourceNodeBase.Name;
                newNodeBase.Description = sourceNodeBase.Description;
            }
        }

        foreach (var connection in sourceCanvas.Connections) {
            var sourcePort = _canvas.Nodes
                                    .SelectMany(n => n.OutputPorts.Concat(n.FlowOutPorts))
                                    .FirstOrDefault(p => p.Id == connection.SourcePortId);
            var targetPort = _canvas.Nodes
                                    .SelectMany(n => n.InputPorts.Concat(n.FlowInPorts))
                                    .FirstOrDefault(p => p.Id == connection.TargetPortId);

            if (sourcePort != null && targetPort != null) {
                Console.WriteLine($"연결 복원 시도: {connection.Guid}");
                _canvas.ConnectWithId(connection.Guid, sourcePort, targetPort);
                Console.WriteLine($"연결 복원 성공: {connection.Guid}");
            }
        }

        foreach (var group in sourceCanvas.Groups) {
            var nodes = group.Nodes
                             .Select(n => _canvas.Nodes.FirstOrDefault(cn => cn.Guid == n.Guid))
                             .Where(n => n != null)
                             .Cast<NodeBase>()
                             .ToList();
            if (nodes.Any()) {
                _canvas.CreateGroupWithId(group.Id, nodes, group.Name);
            }
        }
    }

    private void OnNodeAdded(object? sender, INode node) {
        var nodeViewModel = CreateNodeViewModel(node);
        Nodes.Add(nodeViewModel);
        SelectableItems.Add(nodeViewModel);
    }

    private void OnNodeRemoved(object? sender, INode node) {
        var viewModel = Nodes.FirstOrDefault(vm => vm.Model == node);
        if (viewModel != null) {
            Nodes.Remove(viewModel);
            SelectableItems.Remove(viewModel);
            viewModel.Dispose();
        }
    }

    private void OnConnectionAdded(object? sender, IConnection connection) {
        var connectionViewModel = new ConnectionViewModel(connection, this);
        Connections.Add(connectionViewModel);
        SelectableItems.Add(connectionViewModel);
    }

    private void OnConnectionRemoved(object? sender, IConnection connection) {
        var viewModel = Connections.FirstOrDefault(vm => vm.Model == connection);
        if (viewModel != null) {
            Connections.Remove(viewModel);
            SelectableItems.Remove(viewModel);
            viewModel.Dispose();
        }
    }

    private void OnGroupAdded(object? sender, NodeGroup group) {
        var groupViewModel = new NodeGroupViewModel(group, this);
        Groups.Add(groupViewModel);
    }

    private void OnGroupRemoved(object? sender, NodeGroup group) {
        var viewModel = Groups.FirstOrDefault(vm => vm.Model == group);
        if (viewModel != null) {
            Groups.Remove(viewModel);
            viewModel.Dispose();
        }
    }

    /// <summary>
    /// ViewModel의 컬렉션들을 Model의 상태와 동기화합니다.
    /// </summary>
    private void SynchronizeWithModel() {
        // 컬렉션 동기화 일시 중지
        BindingOperations.DisableCollectionSynchronization(Nodes);
        BindingOperations.DisableCollectionSynchronization(Connections);
        BindingOperations.DisableCollectionSynchronization(Groups);
        BindingOperations.DisableCollectionSynchronization(SelectableItems);

        foreach (var node in Nodes) {
            node.Dispose();
        }

        Nodes.Clear();

        foreach (var node in _canvas.Nodes) {
            var nodeViewModel = CreateNodeViewModel(node);
            Nodes.Add(nodeViewModel);
            SelectableItems.Add(nodeViewModel);
        }

        foreach (var connection in Connections) {
            connection.Dispose();
        }

        Connections.Clear();
        foreach (var connection in _canvas.Connections) {
            var connectionViewModel = new ConnectionViewModel(connection, this);
            Connections.Add(connectionViewModel);
            SelectableItems.Add(connectionViewModel);
        }

        foreach (var group in Groups) {
            group.Dispose();
        }

        Groups.Clear();
        foreach (var group in _canvas.Groups) {
            var groupViewModel = new NodeGroupViewModel(group, this);
            Groups.Add(groupViewModel);
            // 추후 그룹도 ISelectable 구현하면 추가
            // SelectableItems.Add(groupViewModel);
        }

        // 컬렉션 동기화 재개
        BindingOperations.EnableCollectionSynchronization(Nodes, new());
        BindingOperations.EnableCollectionSynchronization(Connections, new());
        BindingOperations.EnableCollectionSynchronization(Groups, new());
        BindingOperations.EnableCollectionSynchronization(SelectableItems, new());
    }

    private NodeViewModel CreateNodeViewModel(INode node) {
        if (node is not NodeBase nodeBase)
            throw new ArgumentException("노드는 NodeBase 타입이어야 합니다.");

        var viewModel = new NodeViewModel(nodeBase, _commandService, this);
        return viewModel;
    }

    private void ExecuteAddNode(Type? nodeType) {
        if (nodeType == null) return;

        var command = new AddNodeCommand(_canvas, nodeType);
        _commandService.Execute(command);
    }

    private void ExecuteAddNodeAt((Type nodeType, double x, double y) args) {
        if (args.nodeType == null) return;

        // 기존 AddNodeCommand 사용
        var command = new AddNodeCommand(_canvas, args.nodeType, args.x, args.y);
        _commandService.Execute(command);
    }

    private void ExecuteRemoveNode(NodeViewModel? nodeViewModel) {
        if (nodeViewModel == null) return;

        var command = new RemoveNodeCommand(_canvas, nodeViewModel.Model);
        _commandService.Execute(command);
    }

    private bool IsValidConnection(NodePortViewModel sourcePort, NodePortViewModel targetPort) {
        if (sourcePort == null || targetPort == null) return false;

        // 같은 노드의 포트인 경우 연결 불가
        var sourceNode = Nodes.FirstOrDefault(n =>
                                                  n.OutputPorts.Contains(sourcePort)
                                               || n.FlowOutPorts.Contains(sourcePort));
        var targetNode = Nodes.FirstOrDefault(n =>
                                                  n.InputPorts.Contains(targetPort)
                                               || n.FlowInPorts.Contains(targetPort));

        if (sourceNode == null || targetNode == null || sourceNode == targetNode)
            return false;

        // 입력-출력 포트 방향이 맞는지 확인
        if (sourcePort.IsInput == targetPort.IsInput)
            return false;

        // 이미 연결된 포트인지 확인
        if (Connections.Any(c =>
                                (c.Source == sourcePort && c.Target == targetPort)
                             || (c.Source == targetPort && c.Target == sourcePort)))
            return false;

        // 포트 타입 호환성 확인
        return sourcePort.CanConnectTo(targetPort);
    }

    private void ExecuteConnect((NodePortViewModel source, NodePortViewModel target) ports) {
        if (!IsValidConnection(ports.source, ports.target))
            return;

        var (source, target) = ports;

        // 입력 포트에 기존 연결이 있는지 확인하고 제거
        var inputPort          = source.IsInput ? source : target;
        var existingConnection = inputPort.Connections.FirstOrDefault()?.Model;
        if (existingConnection != null) {
            _canvas.Disconnect(existingConnection);
        }

        // 새 연결 생성
        var sourcePort = _canvas.Nodes
                                .SelectMany(n => n.OutputPorts.Concat(n.FlowOutPorts))
                                .FirstOrDefault(p => p.Id == ports.source.Id);
        var targetPort = _canvas.Nodes
                                .SelectMany(n => n.InputPorts.Concat(n.FlowInPorts))
                                .FirstOrDefault(p => p.Id == ports.target.Id);

        if (sourcePort != null && targetPort != null) {
            Console.WriteLine($"연결 복원 시도: {ports.source.Id}");
            _canvas.Connect(sourcePort, targetPort);
            Console.WriteLine($"연결 복원 성공: {ports.source.Id}");
        }
    }

    private void ExecuteDisconnect((NodePortViewModel source, NodePortViewModel target) ports) {
        var connection = Connections.FirstOrDefault(c =>
                                                        (c.Source == ports.source && c.Target == ports.target) || (c.Source == ports.target && c.Target == ports.source))
                                    ?.Model;

        if (connection != null) {
            _canvas.Disconnect(connection);
        }
    }

    private void ExecuteUndo() {
        _commandService.Undo();
    }

    private void ExecuteRedo() {
        _commandService.Redo();
    }

    private bool CanExecuteUndo() => _commandService.CanUndo;
    private bool CanExecuteRedo() => _commandService.CanRedo;

    private void AddGroup(NodeGroup? group) {
        if (group == null) return;

        var selectedNodes = Nodes
                            .Where(n => n.IsSelected)
                            .Select(n => (NodeBase)n.Model)
                            .ToList();

        if (selectedNodes.Count != 0) {
            var command = new AddGroupCommand(_canvas, group.Name, selectedNodes);
            _commandService.Execute(command);
        }
    }

    private void RemoveGroup(NodeGroupViewModel? groupVM) {
        if (groupVM?.Model == null) return;

        var command = new RemoveGroupCommand(_canvas, groupVM.Model);
        _commandService.Execute(command);
    }

    public NodeViewModel? FindNodeById(Guid nodeId) {
        return Nodes.FirstOrDefault(n => n.Model.Guid == nodeId);
    }

    public IEnumerable<NodeViewModel> FindNodesByName(string name) {
        return string.IsNullOrWhiteSpace(name) ? [] : Nodes.Where(n => n.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<NodeViewModel> FindNodesByType(Type nodeType) {
        return Nodes.Where(n => nodeType.IsInstanceOfType(n.Model));
    }

    public void Initialize(INodeCommandService commandService) {
        _commandService = commandService;
    }

    public async void ExecuteNodes() {
        try {
            await _canvas.ExecuteAsync();
        }
        catch (Exception ex) {
            MessageBox.Show($"노드 실행 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopySelectedNodes() {
        var selectedNodes = Nodes.Where(n => n.IsSelected).ToList();
        if (!selectedNodes.Any()) return;

        try {
            // 선택된 노드들을 JSON으로 직렬화
            var jsonArray = selectedNodes.Select(vm => vm.Model).NodesToJson();

            // 클립보드에 JSON 저장 (사용자 정의 형식 + 텍스트 형식 모두 지원)
            var dataObject = new DataObject();
            dataObject.SetData(ClipboardFormat, jsonArray);
            dataObject.SetText(jsonArray);
            System.Windows.Clipboard.SetDataObject(dataObject);
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"노드 복사 중 오류: {ex.Message}");
        }
    }

    private void PasteNodes() {
        try {
            // 클립보드에서 데이터 가져오기 (사용자 정의 형식 우선, 없으면 텍스트 형식)
            var     dataObject = System.Windows.Clipboard.GetDataObject();
            string? jsonArray  = null;

            if (dataObject?.GetDataPresent(ClipboardFormat) == true) {
                jsonArray = dataObject.GetData(ClipboardFormat) as string;
            }
            else if (dataObject?.GetDataPresent(DataFormats.Text) == true) {
                jsonArray = dataObject.GetData(DataFormats.Text) as string;
            }

            if (string.IsNullOrEmpty(jsonArray)) return;

            // 모든 노드의 선택 해제
            ClearSelection();

            // 오프셋 계산 - 붙여넣기는 고정된 오프셋 대신 점진적으로 증가
            var pasteOffsetX = 40;
            var pasteOffsetY = 40;

            // JSON에서 노드 생성
            var addedNodes          = _canvas.CreateNodesFromJson(jsonArray, pasteOffsetX, pasteOffsetY);
            var addedNodeViewModels = new List<NodeViewModel>();

            // 생성된 노드의 ViewModel 찾기
            foreach (var node in addedNodes) {
                var nodeVM = Nodes.FirstOrDefault(vm => vm.Id == node.Guid);
                if (nodeVM != null) {
                    addedNodeViewModels.Add(nodeVM);
                }
            }

            // 추가된 노드들 선택
            foreach (var nodeVM in addedNodeViewModels) {
                SelectItem(nodeVM, false);
            }
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"노드 붙여넣기 중 오류: {ex.Message}");
        }
    }

    private void DuplicateSelectedNodes() {
        var selectedNodes = GetSelectedItemsOfType<NodeViewModel>().ToList();
        if (!selectedNodes.Any()) return;

        // 선택 해제
        ClearSelection();

        var addedNodeViewModels = new List<NodeViewModel>();

        // 복제 오프셋 - 복제는 원래 위치에서 좀 더 가까운 거리에 배치
        var duplicateOffsetX = 30;
        var duplicateOffsetY = 30;

        // 각 노드를 복제
        foreach (var nodeVM in selectedNodes) {
            try {
                // 노드를 JSON으로 직렬화 후 새 노드 생성
                var nodeJson = nodeVM.Model.ToJson();
                var newNode  = _canvas.CreateNodeFromJson(nodeJson, duplicateOffsetX, duplicateOffsetY);
                if (newNode != null) {
                    var newNodeVM = Nodes.FirstOrDefault(vm => vm.Id == newNode.Guid);
                    if (newNodeVM != null) {
                        addedNodeViewModels.Add(newNodeVM);
                    }
                }
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"노드 복제 중 오류: {ex.Message}");
            }
        }

        // 복제된 노드들 선택
        foreach (var nodeVM in addedNodeViewModels) {
            SelectItem(nodeVM, false);
        }
    }

    private async void SaveCanvas() {
        var dialog = new Microsoft.Win32.SaveFileDialog {
            Filter     = "Node Canvas Files (*.nodecanvas)|*.nodecanvas|All Files (*.*)|*.*",
            DefaultExt = ".nodecanvas"
        };

        if (dialog.ShowDialog() == true) {
            try {
                await SaveAsync(dialog.FileName);
                MessageBox.Show("캔버스가 저장되었습니다.", "저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) {
                MessageBox.Show($"저장 중 오류가 발생했습니다: {ex.Message}", "저장 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void LoadCanvas() {
        var dialog = new Microsoft.Win32.OpenFileDialog {
            Filter     = "Node Canvas Files (*.nodecanvas)|*.nodecanvas|All Files (*.*)|*.*",
            DefaultExt = ".nodecanvas"
        };

        if (dialog.ShowDialog() == true) {
            try {
                await LoadAsync(dialog.FileName);
                MessageBox.Show("캔버스를 불러왔습니다.", "불러오기 완료", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) {
                MessageBox.Show($"불러오기 중 오류가 발생했습니다: {ex.Message}", "불러오기 오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public async Task SaveAsync(string filePath) {
        ArgumentNullException.ThrowIfNull(filePath);
        await _canvas.SaveToFileAsync(filePath);
    }

    public async Task LoadAsync(string filePath) {
        ArgumentNullException.ThrowIfNull(filePath);
        await _canvas.LoadFromFileAsync(filePath);
        SynchronizeWithModel();
    }

    public Task<string> ToJsonAsync() {
        return Task.FromResult(_canvas.ToJson());
    }

    public async Task LoadFromJsonAsync(string json) {
        ArgumentNullException.ThrowIfNull(json);

        // JSON 파싱은 백그라운드 스레드에서 수행
        var newCanvas = await Task.Run(() => NodeCanvas.FromJson(json));

        // UI 관련 작업은 메인 스레드에서 수행
        var context = SynchronizationContext.Current;
        if (context != null) {
            var tcs = new TaskCompletionSource();
            context.Post(_ => {
                try {
                    // 컬렉션 동기화 일시 중지
                    BindingOperations.DisableCollectionSynchronization(Nodes);
                    BindingOperations.DisableCollectionSynchronization(Connections);
                    BindingOperations.DisableCollectionSynchronization(Groups);
                    BindingOperations.DisableCollectionSynchronization(SelectableItems);

                    UnregisterCanvasEvents();
                    ClearCanvas();

                    // 이벤트 핸들러 등록
                    _canvas.NodeAdded         += OnNodeAdded;
                    _canvas.NodeRemoved       += OnNodeRemoved;
                    _canvas.ConnectionAdded   += OnConnectionAdded;
                    _canvas.ConnectionRemoved += OnConnectionRemoved;
                    _canvas.GroupAdded        += OnGroupAdded;
                    _canvas.GroupRemoved      += OnGroupRemoved;

                    CopyFromCanvas(newCanvas);
                    SynchronizeWithModel();

                    // 컬렉션 동기화 재개
                    BindingOperations.EnableCollectionSynchronization(Nodes, new());
                    BindingOperations.EnableCollectionSynchronization(Connections, new());
                    BindingOperations.EnableCollectionSynchronization(Groups, new());
                    BindingOperations.EnableCollectionSynchronization(SelectableItems, new());

                    tcs.SetResult();
                }
                catch (Exception ex) {
                    tcs.SetException(ex);
                }
            }, null);
            await tcs.Task;
        }
        else {
            UnregisterCanvasEvents();
            ClearCanvas();
            CopyFromCanvas(newCanvas);
            RegisterCanvasEvents();
            SynchronizeWithModel();
        }
    }

    public NodePortViewModel FindPortViewModel(IPort port) {
        foreach (var node in Nodes) {
            var foundPort = node.InputPorts.FirstOrDefault(p => p.Model == port)
                         ?? node.OutputPorts.FirstOrDefault(p => p.Model == port)
                         ?? node.FlowInPorts.FirstOrDefault(p => p.Model == port)
                         ?? node.FlowOutPorts.FirstOrDefault(p => p.Model == port);
            if (foundPort != null)
                return foundPort;
        }

        return null;
    }

    public void OnPortsChanged() {
        // 모든 ConnectionViewModel의 포트 참조 업데이트
        foreach (var connection in Connections) {
            connection.UpdatePortReferences();
        }
    }

    public NodeCanvas Model => _canvas;

    /// <summary>
    /// 항목이 선택되었는지 확인합니다.
    /// </summary>
    /// <param name="item">확인할 항목</param>
    /// <returns>항목이 선택되었으면 true, 아니면 false</returns>
    public bool IsItemSelected(ISelectable item) {
        return SelectedItems.Contains(item);
    }

    /// <summary>
    /// 항목을 선택합니다.
    /// </summary>
    /// <param name="selectable">선택할 항목</param>
    /// <param name="clearOthers">다른 항목의 선택을 해제할지 여부</param>
    public void SelectItem(ISelectable selectable, bool clearOthers = true) {
        if (clearOthers) {
            ClearSelection();
        }

        if (!SelectedItems.Contains(selectable)) {
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
    public void DeselectItem(ISelectable selectable) {
        if (SelectedItems.Contains(selectable)) {
            SelectedItems.Remove(selectable);
            // 선택 변경 이벤트 발생
            OnPropertyChanged(nameof(SelectedItems));
        }
    }

    /// <summary>
    /// 모든 선택 가능한 항목의 선택을 해제합니다.
    /// </summary>
    public void ClearSelection() {
        SelectedItems.Clear();

        // 선택 변경 이벤트 발생
        OnPropertyChanged(nameof(SelectedItems));
    }

    /// <summary>
    /// 선택된 모든 항목을 반환합니다.
    /// </summary>
    public IEnumerable<ISelectable> GetSelectedItems() {
        return SelectedItems;
    }

    /// <summary>
    /// 지정된 유형의 선택된 항목을 반환합니다.
    /// </summary>
    public IEnumerable<T> GetSelectedItemsOfType<T>() where T : ISelectable {
        return SelectedItems.OfType<T>();
    }

    /// <summary>
    /// ID로 항목을 찾아 반환합니다.
    /// </summary>
    public ISelectable? FindSelectableById(Guid id) {
        return SelectableItems.FirstOrDefault(item => item.Id == id);
    }
}
