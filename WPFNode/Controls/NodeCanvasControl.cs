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

namespace WPFNode.Controls;

public class NodeCanvasControl : Control
{
    private NodeViewModel? _dragNode;
    private Point? _lastMousePosition;
    private NodePortViewModel? _dragStartPort;
    private Line? _dragLine;
    private Canvas? _dragCanvas;
    private ScrollViewer? _scrollViewer;
    private SearchPanel? _searchPanel;
    
    private readonly NodeCanvasStateManager _stateManager;
    private bool _isUpdatingLayout;

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
        }

        if (e.NewValue is NodeCanvasViewModel newViewModel)
        {
            _stateManager.Initialize(newViewModel);
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
        if (_dragCanvas == null) return;
        _dragCanvas.Width = window.ActualWidth * 4;
        _dragCanvas.Height = window.ActualHeight * 4;
        
        System.Diagnostics.Debug.WriteLine($"Canvas size updated: {_dragCanvas.Width}x{_dragCanvas.Height} (Window: {window.ActualWidth}x{window.ActualHeight})");
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
        else if (e.Source is NodeControl nodeControl && nodeControl.DataContext is NodeViewModel nodeViewModel)
        {
            _dragNode = nodeViewModel;
            CaptureMouse();
            e.Handled = true;
            return;
        }
        // 캔버스 배경 클릭
        else if (e.Source == this)
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

                case Key.A:
                    SelectAllNodes();
                    e.Handled = true;
                    break;
            }
        }
        else if (e.Key == Key.Delete)
        {
            DeleteSelectedNodes();
            e.Handled = true;
        }
    }

    private void SelectAllNodes()
    {
        if (ViewModel == null) return;
        foreach (var node in ViewModel.Nodes)
        {
            node.IsSelected = true;
        }
    }

    private void CopySelectedNodes()
    {
        if (ViewModel == null) return;
        ViewModel.CopyCommand.Execute(null);
    }

    private void PasteNodes()
    {
        if (ViewModel == null) return;
        ViewModel.PasteCommand.Execute(null);
    }

    private void DeleteSelectedNodes()
    {
        if (ViewModel == null) return;
        var selectedNodes = ViewModel.Nodes.Where(n => n.IsSelected).ToList();
        foreach (var node in selectedNodes)
        {
            ViewModel.RemoveNodeCommand.Execute(node);
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
                ViewModel.AddNodeCommand.Execute(dialog.SelectedNodeType);
            }
        }
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
