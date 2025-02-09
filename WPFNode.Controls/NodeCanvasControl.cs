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
using System.ComponentModel;
using WPFNode.Abstractions;
using WPFNode.Plugin.SDK;
using System.IO;
using System.Collections.Specialized;

namespace WPFNode.Controls;

public class NodeCanvasControl : Control
{
    private NodeViewModel? _dragNode;
    private Point? _lastMousePosition;
    private NodePortViewModel? _dragStartPort;
    private Line? _dragLine;
    private Canvas? _dragCanvas;
    private SearchPanel? _searchPanel;
    
    private readonly NodeCanvasStateManager _stateManager;

    public INodePluginService PluginService { get; }
    public INodeCommandService CommandService { get; }

    public bool IsDraggingPort => _dragStartPort != null;
    public NodePortViewModel? DraggingPort => _dragStartPort;

    private NodeCanvasViewModel? _previousViewModel;

    public NodeCanvasViewModel? ViewModel
    {
        get => (NodeCanvasViewModel?)DataContext;
        set
        {
            if (_previousViewModel != null)
            {
                _stateManager.Cleanup(_previousViewModel);
            }

            var newViewModel = (NodeCanvasViewModel?)value;
            if (newViewModel != null)
            {
                _stateManager.Initialize(newViewModel);
            }

            _previousViewModel = newViewModel;
            DataContext = value;
        }
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NodeCanvasControl control) {
            System.Diagnostics.Debug.WriteLine($"NodeCanvasControl ViewModel changed: {e.NewValue}");
            if (e.NewValue is NodeCanvasViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"Current node count: {viewModel.Nodes.Count}");
            }
        }
    }

    static NodeCanvasControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeCanvasControl),
            new FrameworkPropertyMetadata(typeof(NodeCanvasControl)));
    }

    private class DesignTimeNodePluginService : INodePluginService 
    {
        public IReadOnlyCollection<Type> NodeTypes => new List<Type>();
        public IEnumerable<NodeMetadata> GetAllNodeMetadata() => Enumerable.Empty<NodeMetadata>();
        public IEnumerable<string> GetCategories() => Enumerable.Empty<string>();
        public void LoadPlugins(string pluginPath) { }
        public INode CreateNode(Type nodeType) => throw new NotImplementedException();
        public void RegisterNodeType(Type nodeType) { }
        public IEnumerable<Type> GetNodeTypesByCategory(string category) => Enumerable.Empty<Type>();
        public NodeMetadata GetNodeMetadata(Type nodeType) => throw new NotImplementedException();
        public IEnumerable<NodeMetadata> GetNodeMetadataByCategory(string category) => Enumerable.Empty<NodeMetadata>();
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
        MouseDown += OnMouseButtonDown;
        MouseUp += OnMouseButtonUp;
        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;

        // 키보드 이벤트 처리
        KeyDown += OnKeyDown;

        // 크기 설정
        Width = 800;
        Height = 600;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        System.Diagnostics.Debug.WriteLine($"NodeCanvasControl created: {Width}x{Height}");
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _dragCanvas = GetTemplateChild("PART_Canvas") as Canvas;
        _searchPanel = GetTemplateChild("PART_SearchPanel") as SearchPanel;
        
        if (_searchPanel != null)
        {
            _searchPanel.DataContext = DataContext;
            _searchPanel.PluginService = PluginService;
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
        // 캔버스 드래그
        else if (e.Source == this && ViewModel != null)
        {
            ViewModel.OffsetX += delta.X;
            ViewModel.OffsetY += delta.Y;
            e.Handled = true;
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
} 
