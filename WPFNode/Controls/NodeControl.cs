using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Markup;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Services;
using WPFNode.Utilities;
using WPFNode.ViewModels.Nodes;
using System.Collections.Generic;
using System.Linq;

namespace WPFNode.Controls;

[ContentProperty(nameof(Content))]
public class NodeControl : ContentControl, INodeControl {
    private ContextMenu? _contextMenu;
    private Canvas?      _parentCanvas;

    static NodeControl() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeControl),
                                                 new FrameworkPropertyMetadata(typeof(NodeControl)));
    }

    public NodeViewModel? ViewModel {
        get => (NodeViewModel?)DataContext;
        set => DataContext = value;
    }

    private double CanvasWidth  => ParentCanvas?.ActualWidth ?? 4000;
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

    public double CenteredX {
        get => (double)GetValue(CenteredXProperty);
        private set => SetValue(CenteredXProperty, value);
    }

    public double CenteredY {
        get => (double)GetValue(CenteredYProperty);
        private set => SetValue(CenteredYProperty, value);
    }

    private void UpdateCenteredPosition() {
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

    public object? Content {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public object HeaderContent {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public Brush HeaderBackground {
        get => (Brush)GetValue(HeaderBackgroundProperty);
        set => SetValue(HeaderBackgroundProperty, value);
    }

    public NodeControl() {
        Background      = Brushes.White;
        BorderBrush     = Brushes.Gray;
        BorderThickness = new Thickness(1);

        MouseLeftButtonDown += OnNodeDragStart;
        MouseLeftButtonUp   += OnNodeDragEnd;
        MouseMove           += OnNodeDrag;

        InitializeContextMenu();
        DataContextChanged += OnDataContextChanged;
        Loaded             += OnControlLoaded;
    }

    private void OnControlLoaded(object sender, RoutedEventArgs e) {
        if (ParentCanvas != null) {
            ParentCanvas.SizeChanged += OnCanvasSizeChanged;
        }

        UpdateCenteredPosition();
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e) {
        UpdateCenteredPosition();
    }

    private void InitializeContextMenu() {
        _contextMenu = new ContextMenu();

        var copyMenuItem = new MenuItem { Header = "복사" };
        copyMenuItem.Click += (s, e) => CopySelectedNodes();

        var pasteMenuItem = new MenuItem { Header = "붙여넣기" };
        pasteMenuItem.Click += (s, e) => PasteNodes();

        var duplicateMenuItem = new MenuItem { Header = "복제" };
        duplicateMenuItem.Click += (s, e) => DuplicateSelectedNodes();

        var deleteMenuItem = new MenuItem { Header = "삭제" };
        deleteMenuItem.Click += (s, e) => DeleteSelectedNodes();

        _contextMenu.Items.Add(copyMenuItem);
        _contextMenu.Items.Add(pasteMenuItem);
        _contextMenu.Items.Add(duplicateMenuItem);
        _contextMenu.Items.Add(new Separator());
        _contextMenu.Items.Add(deleteMenuItem);

        ContextMenu = _contextMenu;

        // 키보드 이벤트 핸들러 추가
        PreviewKeyDown += OnNodeKeyDown;
        Focusable      =  true; // 키 이벤트를 받기 위해 필요
    }

    private void OnNodeKeyDown(object sender, KeyEventArgs e) {
        if (Keyboard.Modifiers == ModifierKeys.Control) {
            var canvasViewModel = GetCanvasViewModel();
            if (canvasViewModel == null) return;

            switch (e.Key) {
                case Key.C:
                    canvasViewModel.CopyCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.V:
                    canvasViewModel.PasteCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.D:
                    canvasViewModel.DuplicateCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }

    private INodeCanvasViewModel? GetCanvasViewModel() {
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        return canvas?.ViewModel;
    }

    private void CopySelectedNodes() {
        var canvasViewModel = GetCanvasViewModel();
        if (canvasViewModel != null) {
            canvasViewModel.CopyCommand.Execute(null);
        }
    }

    private void PasteNodes() {
        var canvasViewModel = GetCanvasViewModel();
        if (canvasViewModel != null) {
            canvasViewModel.PasteCommand.Execute(null);
        }
    }

    private void DuplicateSelectedNodes() {
        var canvasViewModel = GetCanvasViewModel();
        if (canvasViewModel != null) {
            canvasViewModel.DuplicateCommand.Execute(null);
        }
    }

    private void DeleteSelectedNodes() {
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas?.ViewModel == null) return;

        var selectedNodes = canvas.ViewModel.GetSelectedItemsOfType<NodeViewModel>().ToList();
        foreach (var node in selectedNodes) {
            ViewModel?.Deselect();
            canvas.ViewModel.RemoveNodeCommand.Execute(node);
        }
    }

    private void OnNodeDragStart(object sender, MouseButtonEventArgs e) {
        if (e.Source is PortControl) return;

        if (ViewModel == null) return;

        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) {
            if (canvas?.ViewModel != null) {
                foreach (var node in canvas.ViewModel.Nodes) {
                    if (node != ViewModel)
                        node.Deselect();
                }
            }
        }

        ViewModel.Select(false);
        var dragPosition = e.GetPosition(this.Parent as IInputElement);
        ViewModel.DragStartPosition = dragPosition;
        ViewModel.StartPosition     = ViewModel.Position;
        
        if (canvas?.ViewModel != null) {
            var selectedNodes = canvas.ViewModel.Nodes.Where(n => n.IsSelected);
            foreach (var node in selectedNodes) {
                node.StartPosition     = node.Position;
                node.DragStartPosition = dragPosition;
            }
        }
        
        CaptureMouse();
        e.Handled = true;
    }

    private void OnNodeDragEnd(object sender, MouseButtonEventArgs e) {
        if (ViewModel != null) {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) {
                var currentPos = e.GetPosition(this.Parent as IInputElement);
                var totalDelta = currentPos - ViewModel.DragStartPosition;

                var canvas = this.GetParentOfType<NodeCanvasControl>();
                if (canvas?.ViewModel != null) {
                    var selectedNodes = canvas.ViewModel.Nodes.Where(n => n.IsSelected);
                    foreach (var node in selectedNodes) {
                        node.Position = new Point(
                            node.StartPosition.X + totalDelta.X,
                            node.StartPosition.Y + totalDelta.Y);
                    }
                }

                UpdateCenteredPosition();
            }

            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void OnNodeDrag(object sender, MouseEventArgs e) {
        if (ViewModel != null && e.LeftButton == MouseButtonState.Pressed) {
            var currentPos = e.GetPosition(this.Parent as IInputElement);

            var canvas = this.GetParentOfType<NodeCanvasControl>();
            if (canvas?.ViewModel != null) {
                var selectedNodes = canvas.ViewModel.GetSelectedItemsOfType<NodeViewModel>();
                var delta         = currentPos - ViewModel.DragStartPosition;
                foreach (var node in selectedNodes) {

                    node.Position = new Point(
                        node.StartPosition.X + delta.X,
                        node.StartPosition.Y + delta.Y);
                }
            }

            UpdateCenteredPosition();
            e.Handled = true;
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
        if (e.OldValue is NodeViewModel oldViewModel) {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is NodeViewModel viewModel) {
            HeaderContent             =  viewModel.Model.Name;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // NodeServices를 통해 PluginService 접근
            var style = WPFNodeServices.UIService.FindNodeStyle(viewModel.Model.GetType());
            if (style != null) {
                // 스타일을 복제하여 사용
                Style = new Style(typeof(NodeControl), style);
            }

            UpdateCenteredPosition();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(NodeViewModel.Position)) {
            UpdateCenteredPosition();
        }
        else if (e.PropertyName == nameof(NodeViewModel.Properties)) { }
    }

    private Canvas? ParentCanvas {
        get {
            if (_parentCanvas == null) {
                var canvas = this.GetParentOfType<NodeCanvasControl>();
                if (canvas != null) {
                    _parentCanvas = canvas.GetDragCanvas();
                }
            }

            return _parentCanvas;
        }
    }

    public PortControl? FindPortControl(NodePortViewModel port) {
        // 모든 ItemsControl 찾기
        var portItemsControls = FindChildrenOfType<ItemsControl>(this);

        // ItemsSource로 포트 컬렉션을 가진 ItemsControl 찾기
        foreach (var itemsControl in portItemsControls) {
            if (itemsControl.ItemsSource is IEnumerable<NodePortViewModel> ports && ports.Contains(port)) {
                var container = itemsControl.ItemContainerGenerator.ContainerFromItem(port) as ContentPresenter;
                if (container == null) continue;

                var portControl = FindChildOfType<PortControl>(container);
                if (portControl != null) {
                    return portControl;
                }
            }
        }

        return null;
    }

    // 자식 요소 중 특정 타입 찾기 (DFS)
    private T? FindChildOfType<T>(DependencyObject parent) where T : DependencyObject {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T result)
                return result;

            var foundInChild = FindChildOfType<T>(child);
            if (foundInChild != null)
                return foundInChild;
        }

        return null;
    }

    // 자식 요소들 중 특정 타입 모두 찾기 - BFS 방식으로 최적화
    private IEnumerable<T> FindChildrenOfType<T>(DependencyObject parent) where T : DependencyObject {
        if (parent == null) yield break;

        var queue = new Queue<DependencyObject>();
        queue.Enqueue(parent);

        while (queue.Count > 0) {
            var current    = queue.Dequeue();
            int childCount = VisualTreeHelper.GetChildrenCount(current);

            for (int i = 0; i < childCount; i++) {
                var child = VisualTreeHelper.GetChild(current, i);

                if (child is T result)
                    yield return result;

                queue.Enqueue(child);
            }
        }
    }
}