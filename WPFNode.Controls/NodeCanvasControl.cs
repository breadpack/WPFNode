using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFNode.Core.Services;
using WPFNode.Core.Interfaces;
using System.Linq;
using System.Collections.Generic;
using WPFNode.Core.Commands;
using WPFNode.Core.ViewModels.Nodes;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using WPFNode.Abstractions;
using WPFNode.Plugin.SDK;

namespace WPFNode.Controls;

public class NodeCanvasControl : Control
{
    private INodePluginService _pluginService;
    private INodeCommandService _commandService;
    private NodeViewModel? _dragNode;
    private Point? _lastMousePosition;
    private NodePortViewModel? _dragStartPort;
    private Line? _dragLine;
    private Canvas? _dragCanvas;
    private SearchPanel? _searchPanel;

    public static readonly DependencyProperty ServiceProviderProperty =
        DependencyProperty.Register(
            nameof(ServiceProvider),
            typeof(IServiceProvider),
            typeof(NodeCanvasControl),
            new PropertyMetadata(null, OnServiceProviderChanged));

    public IServiceProvider? ServiceProvider
    {
        get => (IServiceProvider?)GetValue(ServiceProviderProperty);
        set => SetValue(ServiceProviderProperty, value);
    }

    private static void OnServiceProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NodeCanvasControl control)
        {
            if (e.NewValue is IServiceProvider serviceProvider)
            {
                control.InitializeServices(serviceProvider);
            }
            else if (!DesignerProperties.GetIsInDesignMode(d))
            {
                // ServiceProvider가 설정되지 않은 경우 Application.Current에서 찾아봄
                var app = Application.Current;
                var serviceProviderProperty = app?.GetType().GetProperty("ServiceProvider");
                var appServiceProvider = serviceProviderProperty?.GetValue(app) as IServiceProvider;

                if (appServiceProvider != null)
                {
                    control.InitializeServices(appServiceProvider);
                }
                else
                {
                    throw new InvalidOperationException(
                        "ServiceProvider must be set for NodeCanvasControl in runtime, " +
                        "or Application.Current must have a ServiceProvider property.");
                }
            }
        }
    }

    private void InitializeServices(IServiceProvider serviceProvider)
    {
        _pluginService = serviceProvider.GetRequiredService<INodePluginService>();
        _commandService = serviceProvider.GetRequiredService<INodeCommandService>();
    }

    public INodePluginService PluginService => _pluginService;
    public INodeCommandService CommandService => _commandService;

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(NodeCanvasViewModel),
            typeof(NodeCanvasControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public NodeCanvasViewModel? ViewModel
    {
        get => (NodeCanvasViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    static NodeCanvasControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeCanvasControl),
            new FrameworkPropertyMetadata(typeof(NodeCanvasControl)));
    }

    public static readonly DependencyProperty NameProperty =
        DependencyProperty.Register(
            nameof(Name),
            typeof(string),
            typeof(NodeCanvasControl),
            new PropertyMetadata(null));

    public string? Name
    {
        get => (string?)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NodeCanvasControl control)
        {
            control.DataContext = e.NewValue;
            System.Diagnostics.Debug.WriteLine($"NodeCanvasControl ViewModel changed: {e.NewValue}");
            if (e.NewValue is NodeCanvasViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"Current node count: {viewModel.Nodes.Count}");
            }
        }
    }

    private class DesignTimeNodePluginService : INodePluginService 
    {
        private readonly Dictionary<Type, NodeMetadata> _nodeTypes;
        
        public DesignTimeNodePluginService()
        {
            _nodeTypes = new Dictionary<Type, NodeMetadata>();
            var metadata = new NodeMetadata(typeof(object), "Sample Node", "Basic", "Sample node for design");
            _nodeTypes[typeof(object)] = metadata;
        }

        public IReadOnlyCollection<Type> NodeTypes => _nodeTypes.Keys;
        
        public IEnumerable<NodeMetadata> GetAllNodeMetadata() => _nodeTypes.Values;
        
        public IEnumerable<string> GetCategories() => new[] { "Basic" };
        
        public void LoadPlugins(string pluginPath) { }
        
        public INode CreateNode(Type nodeType) => throw new NotImplementedException();
        
        public void RegisterNodeType(Type nodeType) { }
        
        public IEnumerable<Type> GetNodeTypesByCategory(string category) => 
            _nodeTypes.Where(kvp => kvp.Value.Category == category).Select(kvp => kvp.Key);
        
        public NodeMetadata GetNodeMetadata(Type nodeType) => 
            _nodeTypes.TryGetValue(nodeType, out var metadata) ? metadata : throw new ArgumentException("Node type not found");
        
        public IEnumerable<NodeMetadata> GetNodeMetadataByCategory(string category) =>
            _nodeTypes.Values.Where(m => m.Category == category);
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
            _pluginService = new DesignTimeNodePluginService();
            _commandService = new DesignTimeNodeCommandService();
        }
        else
        {
            // 생성자에서도 ServiceProvider를 찾아봄
            var app = Application.Current;
            var serviceProviderProperty = app?.GetType().GetProperty("ServiceProvider");
            var serviceProvider = serviceProviderProperty?.GetValue(app) as IServiceProvider;

            if (serviceProvider != null)
            {
                InitializeServices(serviceProvider);
            }
        }
        Initialize();
    }

    public NodeCanvasControl(
        INodePluginService pluginService,
        INodeCommandService commandService)
    {
        _pluginService = pluginService ?? throw new ArgumentNullException(nameof(pluginService));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        Initialize();
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
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;

        // 키보드 이벤트 처리
        KeyDown += OnKeyDown;

        // 크기 설정
        Width = 800;
        Height = 600;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        // 디버깅용 메시지
        System.Diagnostics.Debug.WriteLine($"NodeCanvasControl created: {Width}x{Height}");
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _dragCanvas = GetTemplateChild("PART_Canvas") as Canvas;
        _searchPanel = GetTemplateChild("PART_SearchPanel") as SearchPanel;
        
        if (_searchPanel != null)
        {
            _searchPanel.DataContext = ViewModel;
            _searchPanel.ViewModel = ViewModel;
            _searchPanel.PluginService = PluginService;
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
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
        }
        // 노드 클릭 확인
        else if (e.Source is NodeControl nodeControl && nodeControl.DataContext is NodeViewModel nodeViewModel)
        {
            _dragNode = nodeViewModel;
            CaptureMouse();
        }
        // 캔버스 배경 클릭
        else if (e.Source == this)
        {
            CaptureMouse();
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
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
        if (e.LeftButton != MouseButtonState.Pressed || !_lastMousePosition.HasValue) return;

        var currentPosition = e.GetPosition(this);
        var delta = currentPosition - _lastMousePosition.Value;

        // 노드 드래그
        if (_dragNode != null)
        {
            _dragNode.Model.X += delta.X;
            _dragNode.Model.Y += delta.Y;
        }
        // 캔버스 드래그
        else if (_dragStartPort == null && e.Source == this && ViewModel != null)
        {
            ViewModel.OffsetX += delta.X;
            ViewModel.OffsetY += delta.Y;
        }

        _lastMousePosition = currentPosition;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ViewModel == null) return;

        var zoomCenter = e.GetPosition(this);
        var delta = e.Delta * 0.001;
        var newScale = ViewModel.Scale + delta;
        newScale = Math.Max(0.1, Math.Min(2.0, newScale));

        // 줌 중심점 기준으로 스케일 조정
        var dx = zoomCenter.X - ViewModel.OffsetX;
        var dy = zoomCenter.Y - ViewModel.OffsetY;
        
        ViewModel.OffsetX = zoomCenter.X - dx * (newScale / ViewModel.Scale);
        ViewModel.OffsetY = zoomCenter.Y - dy * (newScale / ViewModel.Scale);
        ViewModel.Scale = newScale;
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
        var selectedNodes = ViewModel.Nodes.Where(n => n.IsSelected).ToList();
        if (selectedNodes.Any())
        {
            var nodeDataList = selectedNodes.Select(n => n.Model.CreateCopy()).ToList();
            System.Windows.Clipboard.SetDataObject(nodeDataList);
        }
    }

    private void PasteNodes()
    {
        if (ViewModel == null) return;
        var dataObject = System.Windows.Clipboard.GetDataObject();
        if (dataObject?.GetData(typeof(List<NodeBase>)) is List<NodeBase> nodeDataList)
        {
            foreach (var node in nodeDataList)
            {
                ViewModel.AddNodeCommand.Execute(node);
            }
        }
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
} 
