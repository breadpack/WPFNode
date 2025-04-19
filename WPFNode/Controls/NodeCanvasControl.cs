using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;
using WPFNode.Interfaces;
using WPFNodeCommand = WPFNode.Interfaces.ICommand;
using WPFNode.Models;
using WPFNode.Services;
using WPFNode.ViewModels.Nodes;
using NodeCanvasViewModel = WPFNode.ViewModels.Nodes.NodeCanvasViewModel;
using IWpfCommand = System.Windows.Input.ICommand;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Specialized;
using System.Windows.Markup;
using WPFNode.ViewModels;

namespace WPFNode.Controls;

[ContentProperty("Content")]
public partial class NodeCanvasControl : Control, INodeCanvasControl {
    private NodeViewModel?     _dragNode;
    private Point?             _lastMousePosition;
    private NodePortViewModel? _dragStartPort;
    private Line?              _dragLine;
    private Canvas?            _dragCanvas;
    private ScrollViewer?      _scrollViewer;
    private SearchPanel?       _searchPanel;

    private readonly NodeCanvasStateManager _stateManager;
    private          bool                   _isUpdatingLayout;
    private          Point?                 _contextMenuPosition;
    private          bool                   _initialized = false;
    private          Point                  _panStart;
    private          Point                  _translateOffset;
    private          bool                   _isPanning;
    private          bool                   _isSelecting;
    private          bool                   _isMovingSelection;
    private          Rectangle?             _selectionRectangle;
    private          Point                  _selectionStart;

    #region Dependency Properties

    public static readonly DependencyProperty AutoCenterOnLoadProperty =
        DependencyProperty.Register(
            nameof(AutoCenterOnLoad),
            typeof(bool),
            typeof(NodeCanvasControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty MinScaleProperty =
        DependencyProperty.Register(
            nameof(MinScale),
            typeof(double),
            typeof(NodeCanvasControl),
            new PropertyMetadata(0.25));

    public static readonly DependencyProperty MaxScaleProperty =
        DependencyProperty.Register(
            nameof(MaxScale),
            typeof(double),
            typeof(NodeCanvasControl),
            new PropertyMetadata(2.0));

    public static readonly DependencyProperty ZoomSpeedProperty =
        DependencyProperty.Register(
            nameof(ZoomSpeed),
            typeof(double),
            typeof(NodeCanvasControl),
            new PropertyMetadata(0.001));

    #region Commands

    public static readonly DependencyProperty CenterViewCommandProperty =
        DependencyProperty.Register(
            nameof(CenterViewCommand),
            typeof(IWpfCommand),
            typeof(NodeCanvasControl));

    public static readonly DependencyProperty ZoomToFitCommandProperty =
        DependencyProperty.Register(
            nameof(ZoomToFitCommand),
            typeof(IWpfCommand),
            typeof(NodeCanvasControl));

    public static readonly DependencyProperty ResetViewCommandProperty =
        DependencyProperty.Register(
            nameof(ResetViewCommand),
            typeof(IWpfCommand),
            typeof(NodeCanvasControl));

    public IWpfCommand CenterViewCommand {
        get => (IWpfCommand)GetValue(CenterViewCommandProperty);
        set => SetValue(CenterViewCommandProperty, value);
    }

    public IWpfCommand ZoomToFitCommand {
        get => (IWpfCommand)GetValue(ZoomToFitCommandProperty);
        set => SetValue(ZoomToFitCommandProperty, value);
    }

    public IWpfCommand ResetViewCommand {
        get => (IWpfCommand)GetValue(ResetViewCommandProperty);
        set => SetValue(ResetViewCommandProperty, value);
    }

    #endregion

    public bool AutoCenterOnLoad {
        get => (bool)GetValue(AutoCenterOnLoadProperty);
        set => SetValue(AutoCenterOnLoadProperty, value);
    }

    public double MinScale {
        get => (double)GetValue(MinScaleProperty);
        set => SetValue(MinScaleProperty, value);
    }

    public double MaxScale {
        get => (double)GetValue(MaxScaleProperty);
        set => SetValue(MaxScaleProperty, value);
    }

    public double ZoomSpeed {
        get => (double)GetValue(ZoomSpeedProperty);
        set => SetValue(ZoomSpeedProperty, value);
    }

    public static readonly DependencyProperty PluginServiceProperty =
        DependencyProperty.Register(
            nameof(PluginService),
            typeof(INodeModelService),
            typeof(NodeCanvasControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            "Content",
            typeof(object),
            typeof(NodeCanvasControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(NodeCanvasViewModel),
            typeof(NodeCanvasControl),
            new PropertyMetadata(null, OnViewModelChangedCallback));

    public static readonly DependencyProperty ZoomFactorProperty =
        DependencyProperty.Register(
            "ZoomFactor",
            typeof(double),
            typeof(NodeCanvasControl),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, OnZoomFactorChangedCallback));

    public static readonly DependencyProperty MinZoomProperty =
        DependencyProperty.Register(
            "MinZoom",
            typeof(double),
            typeof(NodeCanvasControl),
            new PropertyMetadata(0.1));

    public static readonly DependencyProperty MaxZoomProperty =
        DependencyProperty.Register(
            "MaxZoom",
            typeof(double),
            typeof(NodeCanvasControl),
            new PropertyMetadata(2.0));

    public static readonly DependencyProperty ModelServiceProperty =
        DependencyProperty.Register(
            nameof(ModelService),
            typeof(INodeModelService),
            typeof(NodeCanvasControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CommandServiceProperty =
        DependencyProperty.Register(
            nameof(CommandService),
            typeof(INodeCommandService),
            typeof(NodeCanvasControl),
            new PropertyMetadata(null));

    public INodeCommandService   CommandService => DesignCommandService;
    public INodeModelService     PluginService  => DesignPluginService;
    public INodeCanvasViewModel? ViewModel      => DataContext as INodeCanvasViewModel;

    private INodeCommandService DesignCommandService { get; set; }
    private INodeModelService   DesignPluginService  { get; set; }

    public object Content {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public double ZoomFactor {
        get => (double)GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    public double MinZoom {
        get => (double)GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom {
        get => (double)GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public INodeModelService? ModelService {
        get => (INodeModelService?)GetValue(ModelServiceProperty);
        set => SetValue(ModelServiceProperty, value);
    }

    public INodeCommandService? NodeCommandService {
        get => (INodeCommandService?)GetValue(CommandServiceProperty);
        set => SetValue(CommandServiceProperty, value);
    }

    #endregion

    public bool               IsDraggingPort => _dragStartPort != null;
    public NodePortViewModel? DraggingPort   => _dragStartPort;

    #region Events

    public event EventHandler<NodeViewModel>?            NodeAdded;
    public event EventHandler<NodeViewModel>?            NodeRemoved;
    public event EventHandler<ConnectionViewModel>?      ConnectionAdded;
    public event EventHandler<ConnectionViewModel>?      ConnectionRemoved;
    public event EventHandler<NodeViewModel>?            NodeMoved;
    public event EventHandler<NodeViewModel>?            NodeSelected;
    public event EventHandler<NodeViewModel>?            NodeDeselected;
    public event EventHandler<ConnectionViewModel>?      ConnectionSelected;
    public event EventHandler<ConnectionViewModel>?      ConnectionDeselected;
    public event EventHandler<ViewportChangedEventArgs>? ViewportChanged;

    private static void OnViewModelChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is NodeCanvasControl control) {
            control.OnDataContextChanged(control, e);
        }
    }

    private static void OnZoomFactorChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        // 아직 구현되지 않음
    }

    protected virtual void OnNodeAdded(NodeViewModel node) {
        NodeAdded?.Invoke(this, node);
    }

    protected virtual void OnNodeRemoved(NodeViewModel node) {
        NodeRemoved?.Invoke(this, node);
    }

    protected virtual void OnConnectionAdded(ConnectionViewModel connection) {
        ConnectionAdded?.Invoke(this, connection);
    }

    protected virtual void OnConnectionRemoved(ConnectionViewModel connection) {
        ConnectionRemoved?.Invoke(this, connection);
    }

    protected virtual void OnNodeMoved(NodeViewModel node) {
        NodeMoved?.Invoke(this, node);
    }

    protected virtual void OnNodeSelected(NodeViewModel node) {
        NodeSelected?.Invoke(this, node);
    }

    protected virtual void OnNodeDeselected(NodeViewModel node) {
        NodeDeselected?.Invoke(this, node);
    }

    protected virtual void OnConnectionSelected(ConnectionViewModel connection) {
        ConnectionSelected?.Invoke(this, connection);
    }

    protected virtual void OnConnectionDeselected(ConnectionViewModel connection) {
        ConnectionDeselected?.Invoke(this, connection);
    }

    protected virtual void OnViewportChanged(double scale, double offsetX, double offsetY) {
        ViewportChanged?.Invoke(this, new ViewportChangedEventArgs(scale, offsetX, offsetY));
    }

    #endregion

    static NodeCanvasControl() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeCanvasControl),
                                                 new FrameworkPropertyMetadata(typeof(NodeCanvasControl)));
    }

    private class DesignTimeNodeModelService : INodeModelService {
        public IReadOnlyCollection<Type> NodeTypes                                    => new List<Type>();
        public IEnumerable<NodeMetadata> GetAllNodeMetadata()                         => Enumerable.Empty<NodeMetadata>();
        public IEnumerable<string>       GetCategories()                              => Enumerable.Empty<string>();
        public void                      LoadPlugins(string               pluginPath) { }
        public INode                     CreateNode(Type                  nodeType)   => throw new NotImplementedException();
        public void                      RegisterNodeType(Type            nodeType)   { }
        public IEnumerable<Type>         GetNodeTypesByCategory(string    category)   => Enumerable.Empty<Type>();
        public NodeMetadata              GetNodeMetadata(Type             nodeType)   => throw new NotImplementedException();
        public IEnumerable<NodeMetadata> GetNodeMetadataByCategory(string category)   => Enumerable.Empty<NodeMetadata>();
    }

    private class DesignTimeNodeCommandService : INodeCommandService {
        private readonly Stack<WPFNodeCommand> _undoStack = new();
        private readonly Stack<WPFNodeCommand> _redoStack = new();
        private          bool                  _isExecuting;

        public event EventHandler?         CanUndoChanged;
        public event EventHandler?         CanRedoChanged;
        public event EventHandler<string>? CommandExecuted;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        // 기존 명령 실행 메서드
        public bool ExecuteCommand(Guid    nodeId, string commandName, object? parameter = null) => false;
        public bool CanExecuteCommand(Guid nodeId, string commandName, object? parameter = null) => false;

        // CommandManager 기능
        public void Execute(WPFNodeCommand command) {
            // 디자인 모드에서는 아무 작업도 수행하지 않음
        }

        public void Undo() {
            // 디자인 모드에서는 아무 작업도 수행하지 않음
        }

        public void Redo() {
            // 디자인 모드에서는 아무 작업도 수행하지 않음
        }

        public void Clear() {
            // 디자인 모드에서는 아무 작업도 수행하지 않음
        }

        public void ExecuteNodeCommand(Guid nodeId, string commandName, object? parameter = null) {
            // 디자인 모드에서는 아무 작업도 수행하지 않음
        }
    }

    public NodeCanvasControl() {
        if (DesignerProperties.GetIsInDesignMode(this)) {
            DesignPluginService  = new DesignTimeNodeModelService();
            DesignCommandService = new DesignTimeNodeCommandService();
        }
        else {
            DesignPluginService  = NodeServices.ModelService;
            DesignCommandService = NodeServices.CommandService;
        }

        _stateManager      =  new NodeCanvasStateManager(this);
        DataContextChanged += OnDataContextChanged;
        Loaded             += OnControlLoaded;

        // 마우스 이벤트 핸들러 연결
        MouseDown  += OnMouseButtonDown;
        MouseUp    += OnMouseButtonUp;
        MouseMove  += OnMouseMove;
        MouseWheel += OnMouseWheel;

        // 키보드 이벤트 핸들러 연결
        KeyDown += OnKeyDown;

        // 컨텍스트 메뉴 초기화
        var contextMenu     = new ContextMenu();
        var addNodeMenuItem = new MenuItem { Header = "노드 추가" };
        addNodeMenuItem.Click += OnAddNodeMenuItemClick;
        contextMenu.Items.Add(addNodeMenuItem);
        ContextMenu = contextMenu;

        // 컨텍스트 메뉴 이벤트 핸들러 연결
        ContextMenuOpening += OnContextMenuOpening;

        // Initialize Commands
        CenterViewCommand = new RelayCommand(CenterView);
        ZoomToFitCommand  = new RelayCommand(ZoomToFit);
        ResetViewCommand  = new RelayCommand(ResetView);

        Initialize();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
        if (e.OldValue is NodeCanvasViewModel oldViewModel) {
            _stateManager.Cleanup(oldViewModel);
            UnsubscribeFromViewModelEvents(oldViewModel);
        }

        if (e.NewValue is NodeCanvasViewModel newViewModel) {
            _stateManager.Initialize(newViewModel);
            SubscribeToViewModelEvents(newViewModel);
        }
    }

    private void SubscribeToViewModelEvents(NodeCanvasViewModel viewModel) {
        viewModel.PropertyChanged                                             += OnViewModelPropertyChanged;
        ((INotifyCollectionChanged)viewModel.Nodes).CollectionChanged         += OnNodesCollectionChanged;
        ((INotifyCollectionChanged)viewModel.Connections).CollectionChanged   += OnConnectionsCollectionChanged;
        ((INotifyCollectionChanged)viewModel.SelectedItems).CollectionChanged += OnSelectedItemsCollectionChanged;
    }

    private void UnsubscribeFromViewModelEvents(NodeCanvasViewModel viewModel) {
        viewModel.PropertyChanged                                             -= OnViewModelPropertyChanged;
        ((INotifyCollectionChanged)viewModel.Nodes).CollectionChanged         -= OnNodesCollectionChanged;
        ((INotifyCollectionChanged)viewModel.Connections).CollectionChanged   -= OnConnectionsCollectionChanged;
        ((INotifyCollectionChanged)viewModel.SelectedItems).CollectionChanged -= OnSelectedItemsCollectionChanged;
    }

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        // 선택된 항목이 추가된 경우
        if (e.NewItems != null) {
            foreach (ISelectable item in e.NewItems) {
                if (item is NodeViewModel node) {
                    OnNodeSelected(node);
                }
                else if (item is ConnectionViewModel connection) {
                    OnConnectionSelected(connection);
                }
            }
        }

        // 선택된 항목이 제거된 경우
        if (e.OldItems != null) {
            foreach (ISelectable item in e.OldItems) {
                if (item is NodeViewModel node) {
                    OnNodeDeselected(node);
                }
                else if (item is ConnectionViewModel connection) {
                    OnConnectionDeselected(connection);
                }
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is NodeCanvasViewModel viewModel) {
            switch (e.PropertyName) {
                case nameof(NodeCanvasViewModel.Scale):
                case nameof(NodeCanvasViewModel.OffsetX):
                case nameof(NodeCanvasViewModel.OffsetY):
                    OnViewportChanged(viewModel.Scale, viewModel.OffsetX, viewModel.OffsetY);
                    break;
            }
        }
        else if (sender is NodeViewModel node && e.PropertyName == nameof(NodeViewModel.Position)) {
            OnNodeMoved(node);
        }
    }

    private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        switch (e.Action) {
            case NotifyCollectionChangedAction.Add:
                foreach (NodeViewModel node in e.NewItems!) {
                    OnNodeAdded(node);
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (NodeViewModel node in e.OldItems!) {
                    OnNodeRemoved(node);
                }

                break;
        }
    }

    private void OnConnectionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        switch (e.Action) {
            case NotifyCollectionChangedAction.Add:
                foreach (ConnectionViewModel connection in e.NewItems!) {
                    OnConnectionAdded(connection);
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (ConnectionViewModel connection in e.OldItems!) {
                    OnConnectionRemoved(connection);
                }

                break;
        }
    }

    private void OnControlLoaded(object sender, RoutedEventArgs e) {
        var window = Window.GetWindow(this);
        if (window != null) {
            UpdateCanvasSize(window);
            window.SizeChanged += OnWindowSizeChanged;
        }

        // 스크롤 위치를 중앙으로 초기화
        if (_scrollViewer != null && _dragCanvas != null && AutoCenterOnLoad) {
            bool          isInitialized        = false;
            EventHandler? layoutUpdatedHandler = null;
            layoutUpdatedHandler = (s, args) => {
                if (!isInitialized && _scrollViewer.ViewportWidth > 0) {
                    _scrollViewer.ScrollToHorizontalOffset((_dragCanvas.Width - _scrollViewer.ViewportWidth) / 2);
                    _scrollViewer.ScrollToVerticalOffset((_dragCanvas.Height - _scrollViewer.ViewportHeight) / 2);
                    isInitialized = true;

                    // 한 번만 실행되도록 이벤트 핸들러 제거
                    if (layoutUpdatedHandler != null)
                        LayoutUpdated -= layoutUpdatedHandler;
                }
            };
            LayoutUpdated += layoutUpdatedHandler;
        }
    }

    public void Initialize() {
        if (_initialized) return;
        _initialized = true;

        var viewModel = DataContext as INodeCanvasViewModel;
        if (viewModel != null && DesignCommandService != null) {
            // Canvas 참조 설정
            if (DesignCommandService is NodeCommandService nodeCommandService && _dragCanvas != null) {
                // 캔버스 뷰모델에 CommandService 설정
                viewModel.Initialize(DesignCommandService);

                // NodeCommandService에 캔버스 설정
                nodeCommandService.SetCanvas(viewModel.Model);
            }
            else {
                viewModel.Initialize(DesignCommandService);
            }

            _initialized = true;
        }
        else {
            // 기본 서비스 설정은 이미 생성자에서 완료됨
        }
    }

    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        _dragCanvas   = GetTemplateChild("PART_Canvas") as Canvas;
        _scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
        _searchPanel  = GetTemplateChild("PART_SearchPanel") as SearchPanel;

        if (_searchPanel != null) {
            _searchPanel.DataContext   = DataContext;
            _searchPanel.PluginService = PluginService;
        }

        if (_dragCanvas != null) {
            _dragCanvas.LayoutUpdated += OnCanvasLayoutUpdated;
        }
    }

    private void UpdateCanvasSize(Window window) {
        if (_dragCanvas == null || _scrollViewer == null) return;

        // 캔버스 크기 변경 전 현재 스크롤 위치의 상대 비율 저장
        double horizontalRatio = 0;
        double verticalRatio   = 0;

        // 스크롤 가능한 영역이 있는 경우에만 비율 계산
        if (_dragCanvas.Width > _scrollViewer.ViewportWidth)
            horizontalRatio = _scrollViewer.HorizontalOffset / (_dragCanvas.Width - _scrollViewer.ViewportWidth);
        if (_dragCanvas.Height > _scrollViewer.ViewportHeight)
            verticalRatio = _scrollViewer.VerticalOffset / (_dragCanvas.Height - _scrollViewer.ViewportHeight);

        // 캔버스 크기 업데이트
        _dragCanvas.Width  = window.ActualWidth * 4;
        _dragCanvas.Height = window.ActualHeight * 4;

        System.Diagnostics.Debug.WriteLine($"Canvas size updated: {_dragCanvas.Width}x{_dragCanvas.Height} (Window: {window.ActualWidth}x{window.ActualHeight})");

        // 스크롤 위치 재조정 - 같은 상대 비율 유지
        if (_dragCanvas.Width > _scrollViewer.ViewportWidth)
            _scrollViewer.ScrollToHorizontalOffset(horizontalRatio * (_dragCanvas.Width - _scrollViewer.ViewportWidth));
        if (_dragCanvas.Height > _scrollViewer.ViewportHeight)
            _scrollViewer.ScrollToVerticalOffset(verticalRatio * (_dragCanvas.Height - _scrollViewer.ViewportHeight));
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e) {
        if (sender is Window window) {
            UpdateCanvasSize(window);
        }
    }

    private bool _pendingConnectionUpdate = false;

    private void OnCanvasLayoutUpdated(object? sender, EventArgs e) {
        if (!_isUpdatingLayout && !_pendingConnectionUpdate) {
            _pendingConnectionUpdate = true;
            Dispatcher.BeginInvoke(new Action(() => {
                try {
                    _isUpdatingLayout = true;
                    // GetVisualDescendants 직접 호출 대신 컨트롤을 직접 찾음
                    UpdateAllConnectionsDirectly();
                }
                finally {
                    _isUpdatingLayout        = false;
                    _pendingConnectionUpdate = false;
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    // ItemsControl에서 직접 ConnectionControl 찾기
    private void UpdateAllConnectionsDirectly() {
        if (_dragCanvas == null) return;

        // 첫 번째 ItemsControl이 일반적으로 연결을 표시
        var connectionItemsControl = FindConnectionsItemsControl();
        if (connectionItemsControl == null) return;

        foreach (ConnectionViewModel connectionVM in connectionItemsControl.Items) {
            var container = connectionItemsControl.ItemContainerGenerator.ContainerFromItem(connectionVM);
            if (container == null) continue;

            // 컨테이너 내에서 ConnectionControl 찾기
            var connectionControl = FindConnectionControlInContainer(container);
            if (connectionControl != null) {
                connectionControl.UpdateConnection();
            }
        }
    }

    // ItemsControl에서 직접 ConnectionControl 찾는 도우미 메서드
    private ItemsControl? FindConnectionsItemsControl() {
        if (_dragCanvas == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(_dragCanvas); i++) {
            var child = VisualTreeHelper.GetChild(_dragCanvas, i);
            if (child is ItemsControl itemsControl && itemsControl.Items.Count > 0 && itemsControl.Items[0] is ConnectionViewModel) {
                return itemsControl;
            }
        }

        return null;
    }

    // 컨테이너에서 ConnectionControl 찾기
    private ConnectionControl? FindConnectionControlInContainer(DependencyObject container) {
        if (container is ConnectionControl connectionControl)
            return connectionControl;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++) {
            var child = VisualTreeHelper.GetChild(container, i);

            if (child is ConnectionControl control)
                return control;

            var foundInChild = FindConnectionControlInContainer(child);
            if (foundInChild != null)
                return foundInChild;
        }

        return null;
    }

    // 특정 노드와 관련된 연결만 업데이트 - 직접 찾기 방식 사용
    public void UpdateConnectionsForNode(NodeViewModel node) {
        if (node == null || _dragCanvas == null) return;

        var connectionItemsControl = FindConnectionsItemsControl();
        if (connectionItemsControl == null) return;

        foreach (ConnectionViewModel connectionVM in connectionItemsControl.Items) {
            // 소스나 타겟 노드가 변경된 노드인 경우만 확인
            var sourcePort = connectionVM.Source;
            var targetPort = connectionVM.Target;

            var sourceNodeViewModel = _stateManager.FindNodeForPort(sourcePort);
            var targetNodeViewModel = _stateManager.FindNodeForPort(targetPort);

            if (sourceNodeViewModel == node || targetNodeViewModel == node) {
                var container = connectionItemsControl.ItemContainerGenerator.ContainerFromItem(connectionVM);
                if (container == null) continue;

                var connectionControl = FindConnectionControlInContainer(container);
                if (connectionControl != null) {
                    connectionControl.UpdateConnection();
                }
            }
        }
    }

    private void OnMouseButtonDown(object sender, MouseButtonEventArgs e) {
        Focus();

        if (e.MiddleButton == MouseButtonState.Pressed) {
            _lastMousePosition = e.GetPosition(this);
            _isPanning         = true;
            Cursor             = Cursors.Hand; // 패닝 중임을 시각적으로 표시
            CaptureMouse();
            e.Handled = true;
            return;
        }

        // --- Start Improved Hit Test ---
        if (_dragCanvas == null) return;

        // Use VisualTreeHelper.HitTest to find the element at the click position
        HitTestResult? hitResult = VisualTreeHelper.HitTest(_dragCanvas, e.GetPosition(_dragCanvas));
        if (hitResult?.VisualHit is DependencyObject hitVisual) {
            // Check if the hit visual or its parent is a PortControl
            var portControl = hitVisual as PortControl ?? GetParentOfType<PortControl>(hitVisual);
            if (portControl != null && portControl.ViewModel != null) {
                // Port click handling is now primarily managed within PortControl itself.
                // We don't initiate the drag line here anymore, as PortControl handles its own drag visuals.
                // Simply capture the mouse to allow PortControl to handle further events.
                CaptureMouse();
                e.Handled = true;
                return;
            }

            // Check if the hit visual or its parent is a NodeControl
            var nodeControl = hitVisual as NodeControl ?? GetParentOfType<NodeControl>(hitVisual);
            if (nodeControl != null && nodeControl.DataContext is NodeViewModel nodeViewModel) {
                _dragNode = nodeViewModel; // Set the node to be dragged
                CaptureMouse();
                e.Handled = true;
                return;
            }
        }
        // --- End Improved Hit Test ---

        // If nothing specific was hit (port or node), treat as canvas click
        // This allows starting selection rectangles or clearing selection (if implemented)
        // 포트 클릭 확인 (기존 코드 삭제)
        // if (e.Source is PortControl portControl && portControl.ViewModel != null)
        // { ... existing port click logic removed ... }

        // 노드 클릭 확인 (기존 코드 삭제)
        // if (e.Source is NodeControl nodeControl && nodeControl.DataContext is NodeViewModel nodeViewModel)
        // { ... existing node click logic removed ... }

        // 캔버스 배경 클릭 (Source check might still be useful for specific canvas background interactions)
        if (e.Source == this || e.Source == _dragCanvas) // Check if the direct source is the canvas itself
        {
            // Clear selection or prepare for selection rectangle start
            // ViewModel?.ClearSelectionCommand.Execute(null); // TODO: Add ClearSelectionCommand to INodeCanvasViewModel and implement it.
            // Potentially start selection rectangle logic here if desired

            CaptureMouse(); // Capture mouse for potential panning or selection rectangle
            e.Handled = true;
        }
    }

    private void OnMouseButtonUp(object sender, MouseButtonEventArgs e) {
        if (e.MiddleButton == MouseButtonState.Released && _isPanning) {
            _isPanning         = false;
            _lastMousePosition = null;
            Cursor             = Cursors.Arrow; // 커서 원래대로 복원
            ReleaseMouseCapture();
            e.Handled = true;
            return;
        }

        // --- Start Cleanup ---
        // Port connection logic is now handled entirely within PortControl's MouseLeftButtonUp
        // based on the improved hit-testing during MouseMove. Remove redundant logic here.
        /*
        if (_dragStartPort != null && e.Source is PortControl portControl && portControl.ViewModel != null)
        {
            var endPort = portControl.ViewModel;
            if (_dragStartPort.IsInput)
            {
                ViewModel?.ConnectCommand.Execute((endPort, _dragStartPort));
            }
            else
            {
                ViewModel?.ConnectCommand.Execute((_dragStartPort, endPort));
            }
        }
        */

        // The drag line (_dragLine) is also managed and removed by PortControl now.
        /*
        if (_dragLine != null && _dragCanvas != null)
        {
            _dragCanvas.Children.Remove(_dragLine);
            _dragLine = null;
        }
        */

        // Reset drag states handled by NodeCanvasControl
        _dragNode          = null;
        _lastMousePosition = null;
        // _dragStartPort should already be null if the drag ended correctly in PortControl, but reset just in case.
        // _dragStartPort = null; // This state is managed by PortControl Start/EndPortDrag

        // Always release mouse capture on button up unless panning was just stopped
        if (!_isPanning) {
            ReleaseMouseCapture();
        }
        // Reset cursor if it was changed for panning (already handled in panning block)
        // Cursor = Cursors.Arrow;

        // Note: We don't set e.Handled = true here unconditionally,
        // as other controls might need to react to MouseUp.
        // However, if a drag operation (_dragNode) was in progress, it should be handled.
        if (_dragNode != null) // Check if we were dragging a node
        {
            e.Handled = true;
        }
        // --- End Cleanup ---
    }

    private void OnMouseMove(object sender, MouseEventArgs e) {
        if (!_lastMousePosition.HasValue) return;

        var currentPosition = e.GetPosition(this);
        var delta           = currentPosition - _lastMousePosition.Value;

        // 포트 드래그 중에는 다른 드래그 동작 무시
        if (_dragStartPort != null) {
            if (_dragLine != null && _dragCanvas != null) {
                var currentPos = e.GetPosition(_dragCanvas);
                if (_dragStartPort.IsInput) {
                    _dragLine.X1 = currentPos.X;
                    _dragLine.Y1 = currentPos.Y;
                }
                else {
                    _dragLine.X2 = currentPos.X;
                    _dragLine.Y2 = currentPos.Y;
                }
            }

            e.Handled = true;
        }
        // 노드 드래그
        else if (_dragNode != null) {
            _dragNode.Model.X += delta.X;
            _dragNode.Model.Y += delta.Y;
            e.Handled         =  true;
        }
        // 캔버스 팬(이동)
        else if (_isPanning && _scrollViewer != null) {
            // 스크롤 위치 조정 - 마우스 이동 방향의 반대로 스크롤
            _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset - delta.X);
            _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - delta.Y);
            e.Handled = true;
        }

        _lastMousePosition = currentPosition;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e) {
        if (ViewModel == null) return;

        var zoomCenter = e.GetPosition(_dragCanvas);
        var delta      = e.Delta * ZoomSpeed;
        var newScale   = ViewModel.Scale + delta;
        newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));

        // 줌 중심점 기준으로 스케일 조정
        var scrollX = _scrollViewer?.HorizontalOffset ?? 0;
        var scrollY = _scrollViewer?.VerticalOffset ?? 0;

        ViewModel.Scale = newScale;

        if (_scrollViewer != null) {
            // 줌 후 스크롤 위치 조정
            var scaleChange = newScale / ViewModel.Scale;
            _scrollViewer.ScrollToHorizontalOffset(scrollX * scaleChange);
            _scrollViewer.ScrollToVerticalOffset(scrollY * scaleChange);
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e) {
        if (ViewModel == null) return;

        if (Keyboard.Modifiers == ModifierKeys.Control) {
            switch (e.Key) {
                case Key.Space:
                    ShowSearchPanel();
                    e.Handled = true;
                    break;

                case Key.C:
                    CopySelectedNodes();
                    e.Handled = true;
                    break;

                case Key.V:
                    PasteNodes();
                    e.Handled = true;
                    break;

                case Key.D:
                    DuplicateSelectedNodes();
                    e.Handled = true;
                    break;

                case Key.A:
                    SelectAllItems();
                    e.Handled = true;
                    break;
            }
        }
        else {
            switch (e.Key) {
                case Key.Delete:
                    DeleteSelectedItems();
                    e.Handled = true;
                    break;

                case Key.Escape:
                    DeSelectAllItems();
                    e.Handled = true;
                    break;
            }
        }
    }

    private void SelectAllItems() {
        if (ViewModel == null) return;

        foreach (var item in ViewModel.SelectableItems) {
            ViewModel.SelectItem(item, false);
        }
    }

    public void DeSelectAllItems() {
        if (ViewModel == null) return;

        foreach (var item in ViewModel.SelectableItems) {
            item.Deselect();
        }
    }

    private void CopySelectedNodes() {
        if (ViewModel == null) return;

        // 선택된 노드만 복사 (현재 ISelectable을 구현해도 노드만 복사 가능하므로)
        var selectedNodes = ViewModel.GetSelectedItemsOfType<NodeViewModel>().ToList();
        if (selectedNodes.Any()) {
            ViewModel.CopyCommand.Execute(null);
        }
    }

    private void PasteNodes() {
        if (ViewModel == null) return;
        ViewModel.PasteCommand.Execute(null);
    }

    private void DuplicateSelectedNodes() {
        if (ViewModel == null) return;
        ViewModel.DuplicateCommand.Execute(null);
    }

    private void DeleteSelectedItems() {
        if (ViewModel == null) return;

        // 선택된 항목 가져오기
        var selectedItems = ViewModel.GetSelectedItems().ToList();

        foreach (var item in selectedItems) {
            switch (item) {
                case NodeViewModel nodeVM:
                    ViewModel.RemoveNodeCommand.Execute(nodeVM);
                    nodeVM.Deselect();
                    break;

                case ConnectionViewModel connectionVM:
                    ViewModel.DisconnectCommand.Execute((connectionVM.Source, connectionVM.Target));
                    connectionVM.Deselect();
                    break;

                // 향후 다른 유형의 선택 가능한 항목이 추가되면 여기에 케이스 추가
            }
        }
    }

    private void ShowSearchPanel() {
        if (_searchPanel != null && ViewModel != null) {
            _searchPanel.Visibility = Visibility.Visible;
            _searchPanel.Focus();
        }
    }

    private void OnAddNodeMenuItemClick(object sender, RoutedEventArgs e) {
        if (PluginService == null) return;
        var dialog = new NodeSelectionDialog(PluginService);
        if (dialog.ShowDialog() == true && dialog.SelectedNodeType != null) {
            var nodeType = dialog.SelectedNodeType;
            if (nodeType != null && ViewModel != null) {
                // 마우스 위치에 노드 생성
                if (_contextMenuPosition.HasValue && _dragCanvas != null) {
                    // 캔버스의 중심을 고려하여 좌표 계산
                    double x = _contextMenuPosition.Value.X - _dragCanvas.Width / 2;
                    double y = _contextMenuPosition.Value.Y - _dragCanvas.Height / 2;

                    // 위치 정보를 포함하여 AddNode 실행
                    ViewModel.AddNodeAtCommand.Execute((nodeType, x, y));
                }
                else {
                    // 위치 정보가 없으면 기본 추가 명령 실행
                    ViewModel.AddNodeCommand.Execute(dialog.SelectedNodeType);
                }
            }
        }
    }

    private void OnContextMenuOpening(object sender, ContextMenuEventArgs e) {
        // 마우스 우클릭 위치 저장
        _contextMenuPosition = Mouse.GetPosition(_dragCanvas);
    }

    internal Canvas? GetDragCanvas() {
        return GetTemplateChild("PART_Canvas") as Canvas;
    }

    public PortControl? FindPortControl(NodePortViewModel port) {
        return _stateManager.FindPortControl(port);
    }

    internal void StartPortDrag(NodePortViewModel port) {
        _dragStartPort = port;
    }

    internal void EndPortDrag() {
        _dragStartPort = null;
    }

    #region Public Methods

    /// <summary>
    /// 뷰를 캔버스의 중앙으로 이동합니다.
    /// </summary>
    public void CenterView() {
        if (_scrollViewer == null || _dragCanvas == null) return;
        _scrollViewer.ScrollToHorizontalOffset((_dragCanvas.Width - _scrollViewer.ViewportWidth) / 2);
        _scrollViewer.ScrollToVerticalOffset((_dragCanvas.Height - _scrollViewer.ViewportHeight) / 2);
    }

    /// <summary>
    /// 모든 노드가 보이도록 뷰를 조정합니다.
    /// </summary>
    public void ZoomToFit() {
        if (ViewModel?.Nodes == null || !ViewModel.Nodes.Any() || _scrollViewer == null || _dragCanvas == null) return;

        // 모든 노드의 영역 계산
        var bounds = GetNodesBounds();
        if (!bounds.HasValue) return;

        // 여백 추가
        var padding = 50;
        bounds = new Rect(
            bounds.Value.X - padding,
            bounds.Value.Y - padding,
            bounds.Value.Width + padding * 2,
            bounds.Value.Height + padding * 2
        );

        // 스케일 계산
        var scaleX = _scrollViewer.ViewportWidth / bounds.Value.Width;
        var scaleY = _scrollViewer.ViewportHeight / bounds.Value.Height;
        var scale  = Math.Min(scaleX, scaleY);
        scale = Math.Max(MinScale, Math.Min(MaxScale, scale));

        // 스케일 적용
        ViewModel.Scale = scale;

        // 중앙 정렬
        _scrollViewer.ScrollToHorizontalOffset(bounds.Value.X * scale + (bounds.Value.Width * scale - _scrollViewer.ViewportWidth) / 2);
        _scrollViewer.ScrollToVerticalOffset(bounds.Value.Y * scale + (bounds.Value.Height * scale - _scrollViewer.ViewportHeight) / 2);
    }

    /// <summary>
    /// 특정 노드로 뷰를 이동합니다.
    /// </summary>
    public void ScrollToNode(NodeViewModel node) {
        if (_scrollViewer == null || _dragCanvas == null) return;

        var nodeX = node.Model.X + _dragCanvas.Width / 2;
        var nodeY = node.Model.Y + _dragCanvas.Height / 2;

        _scrollViewer.ScrollToHorizontalOffset(nodeX - _scrollViewer.ViewportWidth / 2);
        _scrollViewer.ScrollToVerticalOffset(nodeY - _scrollViewer.ViewportHeight / 2);
    }

    /// <summary>
    /// 뷰를 초기 상태로 되돌립니다.
    /// </summary>
    public void ResetView() {
        if (ViewModel == null) return;
        ViewModel.Scale = 1.0;
        CenterView();
    }

    #endregion

    #region Private Methods

    private Rect? GetNodesBounds() {
        if (ViewModel?.Nodes == null || !ViewModel.Nodes.Any()) return null;

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var node in ViewModel.Nodes) {
            minX = Math.Min(minX, node.Model.X);
            minY = Math.Min(minY, node.Model.Y);
            maxX = Math.Max(maxX, node.Model.X);
            maxY = Math.Max(maxY, node.Model.Y);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    #endregion

    #region Private Helper Methods

    // Helper method to find the first ancestor of a given type
    private T? GetParentOfType<T>(DependencyObject? element) where T : DependencyObject {
        if (element == null) return null;
        if (element is T t) return t;
        return GetParentOfType<T>(VisualTreeHelper.GetParent(element));
    }

    #endregion
}