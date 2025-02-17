using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFNode.Utilities;
using WPFNode.ViewModels.Nodes;

namespace WPFNode.Controls;

public class NodeGroupControl : Control
{
    private Point? _dragStart;
    private Point _groupStartPosition;
    private Size _groupStartSize;
    private ResizeMode _currentResizeMode;
    private ContextMenu? _contextMenu;

    static NodeGroupControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeGroupControl),
            new FrameworkPropertyMetadata(typeof(NodeGroupControl)));
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(NodeGroupViewModel),
            typeof(NodeGroupControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public NodeGroupViewModel? ViewModel
    {
        get => (NodeGroupViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NodeGroupControl control)
        {
            control.DataContext = e.NewValue;
        }
    }

    public NodeGroupControl()
    {
        Background = Brushes.Transparent;
        BorderBrush = Brushes.Gray;
        BorderThickness = new Thickness(1);

        MouseLeftButtonDown += OnGroupDragStart;
        MouseLeftButtonUp += OnGroupDragEnd;
        MouseMove += OnGroupDrag;

        InitializeContextMenu();
    }

    private void InitializeContextMenu()
    {
        _contextMenu = new ContextMenu();
        
        var deleteMenuItem = new MenuItem { Header = "삭제" };
        deleteMenuItem.Click += (s, e) => DeleteGroup();

        _contextMenu.Items.Add(deleteMenuItem);
        ContextMenu = _contextMenu;
    }

    private void DeleteGroup()
    {
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas?.ViewModel == null || ViewModel == null) return;

        canvas.ViewModel.RemoveGroupCommand.Execute(ViewModel);
    }

    private void OnGroupDragStart(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel == null) return;

        var position = e.GetPosition(this);
        _currentResizeMode = GetResizeMode(position);

        if (_currentResizeMode == ResizeMode.None)
        {
            _dragStart = e.GetPosition(this.Parent as IInputElement);
            _groupStartPosition = new Point(ViewModel.X, ViewModel.Y);
        }
        else
        {
            _dragStart = e.GetPosition(this.Parent as IInputElement);
            _groupStartPosition = new Point(ViewModel.X, ViewModel.Y);
            _groupStartSize = new Size(ViewModel.Width, ViewModel.Height);
        }

        CaptureMouse();
        e.Handled = true;
    }

    private void OnGroupDragEnd(object sender, MouseButtonEventArgs e)
    {
        _dragStart = null;
        _currentResizeMode = ResizeMode.None;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    private void OnGroupDrag(object sender, MouseEventArgs e)
    {
        if (_dragStart.HasValue && ViewModel != null && IsMouseCaptured)
        {
            var currentPos = e.GetPosition(this.Parent as IInputElement);
            var delta = currentPos - _dragStart.Value;

            if (_currentResizeMode == ResizeMode.None)
            {
                ViewModel.X = _groupStartPosition.X + delta.X;
                ViewModel.Y = _groupStartPosition.Y + delta.Y;
            }
            else
            {
                // 크기 조절
                switch (_currentResizeMode)
                {
                    case ResizeMode.TopLeft:
                        ViewModel.X = _groupStartPosition.X + delta.X;
                        ViewModel.Y = _groupStartPosition.Y + delta.Y;
                        ViewModel.Width = Math.Max(50, _groupStartSize.Width - delta.X);
                        ViewModel.Height = Math.Max(50, _groupStartSize.Height - delta.Y);
                        break;

                    case ResizeMode.TopRight:
                        ViewModel.Y = _groupStartPosition.Y + delta.Y;
                        ViewModel.Width = Math.Max(50, _groupStartSize.Width + delta.X);
                        ViewModel.Height = Math.Max(50, _groupStartSize.Height - delta.Y);
                        break;

                    case ResizeMode.BottomLeft:
                        ViewModel.X = _groupStartPosition.X + delta.X;
                        ViewModel.Width = Math.Max(50, _groupStartSize.Width - delta.X);
                        ViewModel.Height = Math.Max(50, _groupStartSize.Height + delta.Y);
                        break;

                    case ResizeMode.BottomRight:
                        ViewModel.Width = Math.Max(50, _groupStartSize.Width + delta.X);
                        ViewModel.Height = Math.Max(50, _groupStartSize.Height + delta.Y);
                        break;
                }
            }

            e.Handled = true;
        }
    }

    private ResizeMode GetResizeMode(Point position)
    {
        var handleSize = 6.0;
        var rect = new Rect(0, 0, ActualWidth, ActualHeight);

        // 모서리 영역 확인
        if (new Rect(rect.Left, rect.Top, handleSize, handleSize).Contains(position))
            return ResizeMode.TopLeft;
        if (new Rect(rect.Right - handleSize, rect.Top, handleSize, handleSize).Contains(position))
            return ResizeMode.TopRight;
        if (new Rect(rect.Left, rect.Bottom - handleSize, handleSize, handleSize).Contains(position))
            return ResizeMode.BottomLeft;
        if (new Rect(rect.Right - handleSize, rect.Bottom - handleSize, handleSize, handleSize).Contains(position))
            return ResizeMode.BottomRight;

        return ResizeMode.None;
    }

    private enum ResizeMode
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
} 
