using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFNode.Core.Models;
using WPFNode.Core.ViewModels.Nodes;
using System.Windows.Markup;
using WPFNode.Abstractions.Controls;
using System.Reflection;
using WPFNode.Core.Attributes;
using System.Collections.Generic;
using WPFNode.Core.Interfaces;
using WPFNode.Core.Services;

namespace WPFNode.Controls;

[ContentProperty(nameof(Content))]
public class NodeControl : ContentControl, INodeControl
{
    private Point? _dragStart;
    private Point _nodeStartPosition;
    private ContextMenu? _contextMenu;
    private Canvas? _parentCanvas;

    static NodeControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeControl), 
            new FrameworkPropertyMetadata(typeof(NodeControl)));
    }

    public NodeViewModel? ViewModel
    {
        get => (NodeViewModel?)DataContext;
        set => DataContext = value;
    }

    private double CanvasWidth => ParentCanvas?.ActualWidth ?? 4000;
    private double CanvasHeight => ParentCanvas?.ActualHeight ?? 4000;

    public static readonly DependencyProperty CenteredXProperty =
        DependencyProperty.Register(
            nameof(CenteredX),
            typeof(double),
            typeof(NodeControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CenteredYProperty =
        DependencyProperty.Register(
            nameof(CenteredY),
            typeof(double),
            typeof(NodeControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double CenteredX
    {
        get => (double)GetValue(CenteredXProperty);
        private set => SetValue(CenteredXProperty, value);
    }

    public double CenteredY
    {
        get => (double)GetValue(CenteredYProperty);
        private set => SetValue(CenteredYProperty, value);
    }

    private void UpdateCenteredPosition()
    {
        CenteredX = (ViewModel?.Model.X ?? 0) + CanvasWidth / 2;
        CenteredY = (ViewModel?.Model.Y ?? 0) + CanvasHeight / 2;
    }

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(NodeControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty HeaderContentProperty =
        DependencyProperty.Register(
            nameof(HeaderContent),
            typeof(object),
            typeof(NodeControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty HeaderBackgroundProperty =
        DependencyProperty.Register(
            nameof(HeaderBackground),
            typeof(Brush),
            typeof(NodeControl),
            new PropertyMetadata(null));

    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public object HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public Brush HeaderBackground
    {
        get => (Brush)GetValue(HeaderBackgroundProperty);
        set => SetValue(HeaderBackgroundProperty, value);
    }

    public NodeControl()
    {
        Background = Brushes.White;
        BorderBrush = Brushes.Gray;
        BorderThickness = new Thickness(1);

        MouseLeftButtonDown += OnNodeDragStart;
        MouseLeftButtonUp += OnNodeDragEnd;
        MouseMove += OnNodeDrag;

        InitializeContextMenu();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnControlLoaded;
    }

    private void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        if (ParentCanvas != null)
        {
            ParentCanvas.SizeChanged += OnCanvasSizeChanged;
        }
        UpdateCenteredPosition();
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateCenteredPosition();
    }

    private void InitializeContextMenu()
    {
        _contextMenu = new ContextMenu();
        
        var copyMenuItem = new MenuItem { Header = "복사" };
        copyMenuItem.Click += (s, e) => CopySelectedNodes();
        
        var pasteMenuItem = new MenuItem { Header = "붙여넣기" };
        pasteMenuItem.Click += (s, e) => PasteNodes();
        
        var deleteMenuItem = new MenuItem { Header = "삭제" };
        deleteMenuItem.Click += (s, e) => DeleteSelectedNodes();

        _contextMenu.Items.Add(copyMenuItem);
        _contextMenu.Items.Add(pasteMenuItem);
        _contextMenu.Items.Add(new Separator());
        _contextMenu.Items.Add(deleteMenuItem);

        ContextMenu = _contextMenu;
    }

    private void CopySelectedNodes()
    {
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas?.ViewModel == null) return;

        var selectedNodes = canvas.ViewModel.Nodes.Where(n => n.IsSelected).ToList();
        if (selectedNodes.Any())
        {
            var nodeDataList = selectedNodes.Select(n => n.Model.CreateCopy()).ToList();
            Clipboard.SetData("NodeEditorNodes", nodeDataList);
        }
    }

    private void PasteNodes()
    {
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas?.ViewModel == null) return;

        if (Clipboard.GetData("NodeEditorNodes") is List<NodeBase> nodeDataList)
        {
            foreach (var node in nodeDataList)
            {
                canvas.ViewModel.AddNodeCommand.Execute(node);
            }
        }
    }

    private void DeleteSelectedNodes()
    {
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas?.ViewModel == null) return;

        var selectedNodes = canvas.ViewModel.Nodes.Where(n => n.IsSelected).ToList();
        foreach (var node in selectedNodes)
        {
            canvas.ViewModel.RemoveNodeCommand.Execute(node);
        }
    }

    private void OnNodeDragStart(object sender, MouseButtonEventArgs e)
    {
        if (e.Source is PortControl) return;

        if (ViewModel == null) return;

        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            var canvas = this.GetParentOfType<NodeCanvasControl>();
            if (canvas?.ViewModel != null)
            {
                foreach (var node in canvas.ViewModel.Nodes)
                {
                    if (node != ViewModel)
                        node.IsSelected = false;
                }
            }
        }

        ViewModel.IsSelected = true;
        _dragStart = e.GetPosition(this.Parent as IInputElement);
        _nodeStartPosition = ViewModel.Position;
        CaptureMouse();
        e.Handled = true;
    }

    private void OnNodeDragEnd(object sender, MouseButtonEventArgs e)
    {
        if (_dragStart.HasValue && ViewModel != null)
        {
            var currentPos = e.GetPosition(this.Parent as IInputElement);
            var totalDelta = currentPos - _dragStart.Value;

            if (totalDelta.X != 0 || totalDelta.Y != 0)
            {
                var canvas = this.GetParentOfType<NodeCanvasControl>();
                if (canvas?.ViewModel != null)
                {
                    var selectedNodes = canvas.ViewModel.Nodes.Where(n => n.IsSelected);
                    foreach (var node in selectedNodes)
                    {
                        node.Position = new Point(
                            _nodeStartPosition.X + totalDelta.X,
                            _nodeStartPosition.Y + totalDelta.Y);
                    }
                }
                UpdateCenteredPosition();
            }

            _dragStart = null;
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void OnNodeDrag(object sender, MouseEventArgs e)
    {
        if (_dragStart.HasValue && ViewModel != null && IsMouseCaptured)
        {
            var currentPos = e.GetPosition(this.Parent as IInputElement);
            var delta = currentPos - _dragStart.Value;

            var canvas = this.GetParentOfType<NodeCanvasControl>();
            if (canvas?.ViewModel != null)
            {
                foreach (var node in canvas.ViewModel.Nodes)
                {
                    if (node.IsSelected)
                    {
                        node.Position = new Point(
                            _nodeStartPosition.X + delta.X,
                            _nodeStartPosition.Y + delta.Y);
                    }
                }
                UpdateCenteredPosition();
            }

            e.Handled = true;
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is NodeViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is NodeViewModel viewModel)
        {
            HeaderContent = viewModel.Model.Name;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // NodeServices를 통해 PluginService 접근
            var style = NodeServices.PluginService.FindNodeStyle(viewModel.Model.GetType());
            if (style != null)
            {
                // 스타일을 복제하여 사용
                Style = new Style(typeof(NodeControl), style);
            }

            UpdateCenteredPosition();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NodeViewModel.Position))
        {
            UpdateCenteredPosition();
        }
    }

    private Canvas? ParentCanvas
    {
        get
        {
            if (_parentCanvas == null)
            {
                var canvas = this.GetParentOfType<NodeCanvasControl>();
                if (canvas != null)
                {
                    _parentCanvas = canvas.GetDragCanvas();
                }
            }
            return _parentCanvas;
        }
    }

    public PortControl? FindPortControl(NodePortViewModel port)
    {
        var portItemsControl = GetTemplateChild(port.IsInput ? "InputPortsPanel" : "OutputPortsPanel") as ItemsControl;
        if (portItemsControl == null) return null;

        var portContainer = portItemsControl.ItemContainerGenerator
            .ContainerFromItem(port) as ContentPresenter;

        if (portContainer == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(portContainer); i++)
        {
            var child = VisualTreeHelper.GetChild(portContainer, i);
            if (child is PortControl portControl)
                return portControl;
        }

        return null;
    }
} 