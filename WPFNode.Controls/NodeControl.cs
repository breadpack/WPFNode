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

namespace WPFNode.Controls;

[ContentProperty(nameof(Content))]
public class NodeControl : ContentControl, INodeControl
{
    private Point? _dragStart;
    private Point _nodeStartPosition;
    private ContextMenu? _contextMenu;

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
            }

            e.Handled = true;
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is NodeViewModel viewModel)
        {
            var nodeType = viewModel.Model.GetType();
            var styleAttribute = nodeType.GetCustomAttribute<NodeStyleAttribute>();
            
            if (styleAttribute != null)
            {
                var resourceKey = styleAttribute.StyleResourceKey;
                Style? style = null;

                // 1. 현재 컨트롤의 리소스에서 검색
                style = TryFindResource(resourceKey) as Style;

                // 2. 부모 요소들의 리소스에서 검색
                if (style == null)
                {
                    var parent = this.GetParentOfType<NodeCanvasControl>();
                    if (parent != null)
                    {
                        style = parent.TryFindResource(resourceKey) as Style;
                    }
                }

                // 3. Application 리소스에서 검색
                if (style == null)
                {
                    style = Application.Current.TryFindResource(resourceKey) as Style;
                }

                if (style != null)
                {
                    Style = style;
                }
            }

            HeaderContent = viewModel.Model.Name;
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