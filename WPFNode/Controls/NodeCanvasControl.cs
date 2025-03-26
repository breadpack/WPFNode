using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Services;
using WPFNode.Utilities;
using WPFNode.ViewModels.Nodes;
using NodeCanvasViewModel = WPFNode.ViewModels.Nodes.NodeCanvasViewModel;
using IWpfCommand = System.Windows.Input.ICommand;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Specialized;

namespace WPFNode.Controls;

public class ViewportChangedEventArgs : EventArgs
{
    public double Scale { get; }
    public double OffsetX { get; }
    public double OffsetY { get; }

    public ViewportChangedEventArgs(double scale, double offsetX, double offsetY)
    {
        Scale = scale;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
}

public class NodeCanvasControl : Control
{
    private NodeViewModel?     _dragNode;
    private Point?             _lastMousePosition;
    private NodePortViewModel? _dragStartPort;
    private Line?              _dragLine;
    private Canvas?            _dragCanvas;
    private ScrollViewer?      _scrollViewer;
    private SearchPanel?       _searchPanel;
    
    private readonly NodeCanvasStateManager _stateManager;
    private bool _isUpdatingLayout;
    private Point? _contextMenuPosition;

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

    public IWpfCommand CenterViewCommand
    {
        get => (IWpfCommand)GetValue(CenterViewCommandProperty);
        set => SetValue(CenterViewCommandProperty, value);
    }

    public IWpfCommand ZoomToFitCommand
    {
        get => (IWpfCommand)GetValue(ZoomToFitCommandProperty);
        set => SetValue(ZoomToFitCommandProperty, value);
    }

    public IWpfCommand ResetViewCommand
    {
        get => (IWpfCommand)GetValue(ResetViewCommandProperty);
        set => SetValue(ResetViewCommandProperty, value);
    }

    #endregion

    public bool AutoCenterOnLoad
    {
        get => (bool)GetValue(AutoCenterOnLoadProperty);
        set => SetValue(AutoCenterOnLoadProperty, value);
    }

    public double MinScale
    {
        get => (double)GetValue(MinScaleProperty);
        set => SetValue(MinScaleProperty, value);
    }

    public double MaxScale
    {
        get => (double)GetValue(MaxScaleProperty);
        set => SetValue(MaxScaleProperty, value);
    }

    public double ZoomSpeed
    {
        get => (double)GetValue(ZoomSpeedProperty);
        set => SetValue(ZoomSpeedProperty, value);
    }

    #endregion

    public INodePluginService PluginService { get; }
    public INodeCommandService CommandService { get; }

    public bool IsDraggingPort => _dragStartPort != null;
    public NodePortViewModel? DraggingPort => _dragStartPort;

    public INodeCanvasViewModel? ViewModel => DataContext as INodeCanvasViewModel;

    #region Events
    public event EventHandler<NodeViewModel>? NodeAdded;
    public event EventHandler<NodeViewModel>? NodeRemoved;
    public event EventHandler<ConnectionViewModel>? ConnectionAdded;
    public event EventHandler<ConnectionViewModel>? ConnectionRemoved;
    public event EventHandler<NodeViewModel>? NodeMoved;
    public event EventHandler<NodeViewModel>? NodeSelected;
    public event EventHandler<NodeViewModel>? NodeDeselected;
    public event EventHandler<ConnectionViewModel>? ConnectionSelected;
    public event EventHandler<ConnectionViewModel>? ConnectionDeselected;
    public event EventHandler<ViewportChangedEventArgs>? ViewportChanged;

    protected virtual void OnNodeAdded(NodeViewModel node)
    {
        NodeAdded?.Invoke(this, node);
    }

    protected virtual void OnNodeRemoved(NodeViewModel node)
    {
        NodeRemoved?.Invoke(this, node);
    }

    protected virtual void OnConnectionAdded(ConnectionViewModel connection)
    {
        ConnectionAdded?.Invoke(this, connection);
    }

    protected virtual void OnConnectionRemoved(ConnectionViewModel connection)
    {
        ConnectionRemoved?.Invoke(this, connection);
    }

    protected virtual void OnNodeMoved(NodeViewModel node)
    {
        NodeMoved?.Invoke(this, node);
    }

    protected virtual void OnNodeSelected(NodeViewModel node)
    {
        NodeSelected?.Invoke(this, node);
    }

    protected virtual void OnNodeDeselected(NodeViewModel node)
    {
        NodeDeselected?.Invoke(this, node);
    }

    protected virtual void OnConnectionSelected(ConnectionViewModel connection)
    {
        ConnectionSelected?.Invoke(this, connection);
    }

    protected virtual void OnConnectionDeselected(ConnectionViewModel connection)
    {
        ConnectionDeselected?.Invoke(this, connection);
    }

    protected virtual void OnViewportChanged(double scale, double offsetX, double offsetY)
    {
        ViewportChanged?.Invoke(this, new ViewportChangedEventArgs(scale, offsetX, offsetY));
    }
    #endregion

    static NodeCanvasControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeCanvasControl),
            new FrameworkPropertyMetadata(typeof(NodeCanvasControl)));
    }

    private class DesignTimeNodePluginService : INodePluginService 
    {
        public IReadOnlyCollection<Type> NodeTypes                                    => new List<Type>();
        public IEnumerable<NodeMetadata> GetAllNodeMetadata()                         => Enumerable.Empty<NodeMetadata>();
        public Style?                    FindNodeStyle(Type nodeType)                 => null;
        public IEnumerable<string>       GetCategories()                              => Enumerable.Empty<string>();
        public void                      LoadPlugins(string               pluginPath) { }
        public INode                     CreateNode(Type                  nodeType)   => throw new NotImplementedException();
        public void                      RegisterNodeType(Type            nodeType)   { }
        public IEnumerable<Type>         GetNodeTypesByCategory(string    category)   => Enumerable.Empty<Type>();
        public NodeMetadata              GetNodeMetadata(Type             nodeType)   => throw new NotImplementedException();
        public IEnumerable<NodeMetadata> GetNodeMetadataByCategory(string category)   => Enumerable.Empty<NodeMetadata>();
    }

    private class DesignTimeNodeCommandService : INodeCommandService 
    {
        public void RegisterNode(INode node) { }
        public void UnregisterNode(Guid nodeId) { }
        public bool ExecuteCommand(Guid nodeId, string commandName, object? parameter = null) => false;
        public bool CanExecuteCommand(Guid nodeId, string commandName, object? parameter = null) => false;
    }

    public NodeCanvasControl()
    {
        if (DesignerProperties.GetIsInDesignMode(this))
        {
            PluginService = new DesignTimeNodePluginService();
            CommandService = new DesignTimeNodeCommandService();
        }
        else
        {
            PluginService = NodeServices.PluginService;
            CommandService = NodeServices.CommandService;
        }
        
        _stateManager = new NodeCanvasStateManager(this);
        DataContextChanged += OnDataContextChanged;
        Loaded += OnControlLoaded;

        // Initialize Commands
        CenterViewCommand = new RelayCommand(CenterView);
        ZoomToFitCommand = new RelayCommand(ZoomToFit);
        ResetViewCommand = new RelayCommand(ResetView);

        Initialize();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is NodeCanvasViewModel oldViewModel)
        {
            _stateManager.Cleanup(oldViewModel);
            UnsubscribeFromViewModelEvents(oldViewModel);
        }

        if (e.NewValue is NodeCanvasViewModel newViewModel)
        {
            _stateManager.Initialize(newViewModel);
            SubscribeToViewModelEvents(newViewModel);
        }
    }

    private void SubscribeToViewModelEvents(NodeCanvasViewModel viewModel)
    {
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ((INotifyCollectionChanged)viewModel.Nodes).CollectionChanged += OnNodesCollectionChanged;
        ((INotifyCollectionChanged)viewModel.Connections).CollectionChanged += OnConnectionsCollectionChanged;
        ((INotifyCollectionChanged)viewModel.SelectedItems).CollectionChanged += OnSelectedItemsCollectionChanged;
    }

    private void UnsubscribeFromViewModelEvents(NodeCanvasViewModel viewModel)
    {
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ((INotifyCollectionChanged)viewModel.Nodes).CollectionChanged -= OnNodesCollectionChanged;
        ((INotifyCollectionChanged)viewModel.Connections).CollectionChanged -= OnConnectionsCollectionChanged;
        ((INotifyCollectionChanged)viewModel.SelectedItems).CollectionChanged -= OnSelectedItemsCollectionChanged;
    }

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 선택된 항목이 추가된 경우
        if (e.NewItems != null)
        {
            foreach (ISelectable item in e.NewItems)
            {
                if (item is NodeViewModel node)
                {
                    OnNodeSelected(node);
                }
                else if (item is ConnectionViewModel connection)
                {
                    OnConnectionSelected(connection);
                }
            }
        }
        
        // 선택된 항목이 제거된 경우
        if (e.OldItems != null)
        {
            foreach (ISelectable item in e.OldItems)
            {
                if (item is NodeViewModel node)
                {
                    OnNodeDeselected(node);
                }
                else if (item is ConnectionViewModel connection)
                {
                    OnConnectionDeselected(connection);
                }
            }
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is NodeCanvasViewModel viewModel)
        {
            switch (e.PropertyName)
            {
                case nameof(NodeCanvasViewModel.Scale):
                case nameof(NodeCanvasViewModel.OffsetX):
                case nameof(NodeCanvasViewModel.OffsetY):
                    OnViewportChanged(viewModel.Scale, viewModel.OffsetX, viewModel.OffsetY);
                    break;
            }
        }
        else if (sender is NodeViewModel node && e.PropertyName == nameof(NodeViewModel.Position))
        {
            OnNodeMoved(node);
        }
    }

    private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (NodeViewModel node in e.NewItems!)
                {
                    OnNodeAdded(node);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (NodeViewModel node in e.OldItems!)
                {
                    OnNodeRemoved(node);
                }
                break;
        }
    }

    private void OnConnectionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (ConnectionViewModel connection in e.NewItems!)
                {
                    OnConnectionAdded(connection);
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (ConnectionViewModel connection in e.OldItems!)
                {
                    OnConnectionRemoved(connection);
                }
                break;
        }
    }

    private void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window != null)
        {
            UpdateCanvasSize(window);
            window.SizeChanged += OnWindowSizeChanged;
        }

        // 스크롤 위치를 중앙으로 초기화
        if (_scrollViewer != null && _dragCanvas != null && AutoCenterOnLoad)
        {
            bool isInitialized = false;
            EventHandler? layoutUpdatedHandler = null;
            layoutUpdatedHandler = (s, args) =>
            {
                if (!isInitialized && _scrollViewer.ViewportWidth > 0)
                {
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

    private void Initialize()
    {
        Focusable = true;
        Focus();
        Background = Brushes.LightGray;
        
        // ContextMenu 설정
        ContextMenu = new ContextMenu();
        var addNodeMenuItem = new MenuItem { Header = "노드 추가" };
        addNodeMenuItem.Click += OnAddNodeMenuItemClick;
        var managePluginsMenuItem = new MenuItem { Header = "플러그인 관리" };
        managePluginsMenuItem.Click += OnManagePluginsClick;
        ContextMenu.Items.Add(addNodeMenuItem);
        ContextMenu.Items.Add(new Separator());
        ContextMenu.Items.Add(managePluginsMenuItem);
        
        // ContextMenu를 열 때 위치 저장
        ContextMenuOpening += OnContextMenuOpening;
        
        // 마우스 이벤트 처리
        MouseDown += OnMouseButtonDown;
        MouseUp += OnMouseButtonUp;
        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;

        // 키보드 이벤트 처리
        KeyDown += OnKeyDown;

        System.Diagnostics.Debug.WriteLine($"NodeCanvasControl created: {Width}x{Height}");
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _dragCanvas = GetTemplateChild("PART_Canvas") as Canvas;
        _scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
        _searchPanel = GetTemplateChild("PART_SearchPanel") as SearchPanel;
        
        if (_searchPanel != null)
        {
            _searchPanel.DataContext = DataContext;
            _searchPanel.PluginService = PluginService;
        }

        if (_dragCanvas != null)
        {
            _dragCanvas.LayoutUpdated += OnCanvasLayoutUpdated;
        }
    }

    private void UpdateCanvasSize(Window window)
    {
        if (_dragCanvas == null || _scrollViewer == null) return;
        
        // 캔버스 크기 변경 전 현재 스크롤 위치의 상대 비율 저장
        double horizontalRatio = 0;
        double verticalRatio = 0;
        
        // 스크롤 가능한 영역이 있는 경우에만 비율 계산
        if (_dragCanvas.Width > _scrollViewer.ViewportWidth)
            horizontalRatio = _scrollViewer.HorizontalOffset / (_dragCanvas.Width - _scrollViewer.ViewportWidth);
        if (_dragCanvas.Height > _scrollViewer.ViewportHeight)
            verticalRatio = _scrollViewer.VerticalOffset / (_dragCanvas.Height - _scrollViewer.ViewportHeight);
        
        // 캔버스 크기 업데이트
        _dragCanvas.Width = window.ActualWidth * 4;
        _dragCanvas.Height = window.ActualHeight * 4;
        
        System.Diagnostics.Debug.WriteLine($"Canvas size updated: {_dragCanvas.Width}x{_dragCanvas.Height} (Window: {window.ActualWidth}x{window.ActualHeight})");
        
        // 스크롤 위치 재조정 - 같은 상대 비율 유지
        if (_dragCanvas.Width > _scrollViewer.ViewportWidth)
            _scrollViewer.ScrollToHorizontalOffset(horizontalRatio * (_dragCanvas.Width - _scrollViewer.ViewportWidth));
        if (_dragCanvas.Height > _scrollViewer.ViewportHeight)
            _scrollViewer.ScrollToVerticalOffset(verticalRatio * (_dragCanvas.Height - _scrollViewer.ViewportHeight));
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Window window)
        {
            UpdateCanvasSize(window);
        }
    }

    private void OnCanvasLayoutUpdated(object? sender, EventArgs e)
    {
        if (!_isUpdatingLayout)
        {
            try
            {
                _isUpdatingLayout = true;
                UpdateAllConnections();
            }
            finally
            {
                _isUpdatingLayout = false;
            }
        }
    }

    public void UpdateAllConnections()
    {
        foreach (var connection in this.GetVisualDescendants().OfType<ConnectionControl>())
        {
            connection.UpdateConnection();
        }
    }

    private void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        if(e.MiddleButton != MouseButtonState.Pressed) return;
        
        Focus();
        _lastMousePosition = e.GetPosition(this);

        // 포트 클릭 확인
        if (e.Source is PortControl portControl && portControl.ViewModel != null)
        {
            _dragStartPort = portControl.ViewModel;
            _dragLine = new Line
            {
                Stroke = Brushes.Gray,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection(new[] { 4d, 2d })
            };

            if (_dragCanvas != null)
            {
                _dragCanvas.Children.Add(_dragLine);
                var portPosition = portControl.TranslatePoint(new Point(6, 6), _dragCanvas);
                if (_dragStartPort.IsInput)
                {
                    _dragLine.X2 = portPosition.X;
                    _dragLine.Y2 = portPosition.Y;
                }
                else
                {
                    _dragLine.X1 = portPosition.X;
                    _dragLine.Y1 = portPosition.Y;
                }
            }
            CaptureMouse();
            e.Handled = true;
            return;
        }
        
        // 노드 클릭 확인
        if (e.Source is NodeControl nodeControl && nodeControl.DataContext is NodeViewModel nodeViewModel)
        {
            _dragNode = nodeViewModel;
            CaptureMouse();
            e.Handled = true;
            return;
        }
        
        // 캔버스 배경 클릭
        if (e.Source == this)
        {
            CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnMouseButtonUp(object sender, MouseButtonEventArgs e)
    {
        if(e.MiddleButton != MouseButtonState.Released) return;
        
        // 포트 연결 처리
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

        if (_dragLine != null && _dragCanvas != null)
        {
            _dragCanvas.Children.Remove(_dragLine);
            _dragLine = null;
        }

        _dragStartPort = null;
        _dragNode = null;
        _lastMousePosition = null;
        ReleaseMouseCapture();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.MiddleButton != MouseButtonState.Pressed || !_lastMousePosition.HasValue) return;

        var currentPosition = e.GetPosition(this);
        var delta = currentPosition - _lastMousePosition.Value;

        // 포트 드래그 중에는 다른 드래그 동작 무시
        if (_dragStartPort != null)
        {
            if (_dragLine != null && _dragCanvas != null)
            {
                var currentPos = e.GetPosition(_dragCanvas);
                if (_dragStartPort.IsInput)
                {
                    _dragLine.X1 = currentPos.X;
                    _dragLine.Y1 = currentPos.Y;
                }
                else
                {
                    _dragLine.X2 = currentPos.X;
                    _dragLine.Y2 = currentPos.Y;
                }
            }
            e.Handled = true;
            return;
        }
        // 노드 드래그
        else if (_dragNode != null)
        {
            _dragNode.Model.X += delta.X;
            _dragNode.Model.Y += delta.Y;
            e.Handled = true;
        }
        // 캔버스 드래그 (스크롤 조정)
        else if (e.Source == this && _scrollViewer != null)
        {
            // 스크롤 위치 조정
            _scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset - delta.X);
            _scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset - delta.Y);
            
            e.Handled = true;
        }

        _lastMousePosition = currentPosition;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ViewModel == null) return;

        var zoomCenter = e.GetPosition(_dragCanvas);
        var delta = e.Delta * ZoomSpeed;
        var newScale = ViewModel.Scale + delta;
        newScale = Math.Max(MinScale, Math.Min(MaxScale, newScale));

        // 줌 중심점 기준으로 스케일 조정
        var scrollX = _scrollViewer?.HorizontalOffset ?? 0;
        var scrollY = _scrollViewer?.VerticalOffset ?? 0;
        
        ViewModel.Scale = newScale;

        if (_scrollViewer != null)
        {
            // 줌 후 스크롤 위치 조정
            var scaleChange = newScale / ViewModel.Scale;
            _scrollViewer.ScrollToHorizontalOffset(scrollX * scaleChange);
            _scrollViewer.ScrollToVerticalOffset(scrollY * scaleChange);
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel == null) return;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
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
        else if (e.Key == Key.Delete)
        {
            DeleteSelectedItems();
            e.Handled = true;
        }
    }

    private void SelectAllItems()
    {
        if (ViewModel == null) return;
        
        foreach (var item in ViewModel.SelectableItems)
        {
            ViewModel.SelectItem(item, false);
        }
    }

    private void CopySelectedNodes()
    {
        if (ViewModel == null) return;
        
        // 선택된 노드만 복사 (현재 ISelectable을 구현해도 노드만 복사 가능하므로)
        var selectedNodes = ViewModel.GetSelectedItemsOfType<NodeViewModel>().ToList();
        if (selectedNodes.Any())
        {
            ViewModel.CopyCommand.Execute(null);
        }
    }

    private void PasteNodes()
    {
        if (ViewModel == null) return;
        ViewModel.PasteCommand.Execute(null);
    }
    
    private void DuplicateSelectedNodes()
    {
        if (ViewModel == null) return;
        ViewModel.DuplicateCommand.Execute(null);
    }

    private void DeleteSelectedItems()
    {
        if (ViewModel == null) return;
        
        // 선택된 항목 가져오기
        var selectedItems = ViewModel.GetSelectedItems().ToList();
        
        foreach (var item in selectedItems)
        {
            switch (item)
            {
                case NodeViewModel nodeVM:
                    ViewModel.RemoveNodeCommand.Execute(nodeVM);
                    break;
                    
                case ConnectionViewModel connectionVM:
                    ViewModel.DisconnectCommand.Execute((connectionVM.Source, connectionVM.Target));
                    break;
                    
                // 향후 다른 유형의 선택 가능한 항목이 추가되면 여기에 케이스 추가
            }
        }
    }

    private void ShowSearchPanel()
    {
        if (_searchPanel != null && ViewModel != null)
        {
            _searchPanel.Visibility = Visibility.Visible;
            _searchPanel.Focus();
        }
    }

    private void OnManagePluginsClick(object sender, RoutedEventArgs e)
    {
        if (PluginService == null) return;
        var dialog = new NodePluginManagerDialog(PluginService);
        dialog.ShowDialog();
    }

    private void OnAddNodeMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (PluginService == null) return;
        var dialog = new NodeSelectionDialog(PluginService);
        if (dialog.ShowDialog() == true && dialog.SelectedNodeType != null)
        {
            var nodeType = dialog.SelectedNodeType;
            if (nodeType != null && ViewModel != null)
            {
                // 마우스 위치에 노드 생성
                if (_contextMenuPosition.HasValue && _dragCanvas != null)
                {
                    // 캔버스의 중심을 고려하여 좌표 계산
                    double x = _contextMenuPosition.Value.X - _dragCanvas.Width / 2;
                    double y = _contextMenuPosition.Value.Y - _dragCanvas.Height / 2;
                    
                    // 위치 정보를 포함하여 AddNode 실행
                    ViewModel.AddNodeAtCommand.Execute((nodeType, x, y));
                }
                else
                {
                    // 위치 정보가 없으면 기본 추가 명령 실행
                    ViewModel.AddNodeCommand.Execute(dialog.SelectedNodeType);
                }
            }
        }
    }

    private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        // 마우스 우클릭 위치 저장
        _contextMenuPosition = Mouse.GetPosition(_dragCanvas);
    }

    internal Canvas? GetDragCanvas()
    {
        return GetTemplateChild("PART_Canvas") as Canvas;
    }

    public PortControl? FindPortControl(NodePortViewModel port)
    {
        return _stateManager.FindPortControl(port);
    }

    internal void StartPortDrag(NodePortViewModel port)
    {
        _dragStartPort = port;
    }

    internal void EndPortDrag()
    {
        _dragStartPort = null;
    }

    #region Public Methods

    /// <summary>
    /// 뷰를 캔버스의 중앙으로 이동합니다.
    /// </summary>
    public void CenterView()
    {
        if (_scrollViewer == null || _dragCanvas == null) return;
        _scrollViewer.ScrollToHorizontalOffset((_dragCanvas.Width - _scrollViewer.ViewportWidth) / 2);
        _scrollViewer.ScrollToVerticalOffset((_dragCanvas.Height - _scrollViewer.ViewportHeight) / 2);
    }

    /// <summary>
    /// 모든 노드가 보이도록 뷰를 조정합니다.
    /// </summary>
    public void ZoomToFit()
    {
        if (ViewModel?.Nodes == null || !ViewModel.Nodes.Any() || 
            _scrollViewer == null || _dragCanvas == null) return;

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
        var scale = Math.Min(scaleX, scaleY);
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
    public void ScrollToNode(NodeViewModel node)
    {
        if (_scrollViewer == null || _dragCanvas == null) return;

        var nodeX = node.Model.X + _dragCanvas.Width / 2;
        var nodeY = node.Model.Y + _dragCanvas.Height / 2;

        _scrollViewer.ScrollToHorizontalOffset(nodeX - _scrollViewer.ViewportWidth / 2);
        _scrollViewer.ScrollToVerticalOffset(nodeY - _scrollViewer.ViewportHeight / 2);
    }

    /// <summary>
    /// 뷰를 초기 상태로 되돌립니다.
    /// </summary>
    public void ResetView()
    {
        if (ViewModel == null) return;
        ViewModel.Scale = 1.0;
        CenterView();
    }

    #endregion

    #region Private Methods

    private Rect? GetNodesBounds()
    {
        if (ViewModel?.Nodes == null || !ViewModel.Nodes.Any()) return null;

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var node in ViewModel.Nodes)
        {
            minX = Math.Min(minX, node.Model.X);
            minY = Math.Min(minY, node.Model.Y);
            maxX = Math.Max(maxX, node.Model.X);
            maxY = Math.Max(maxY, node.Model.Y);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    #endregion
}
