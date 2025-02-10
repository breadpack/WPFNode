using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFNode.Core.ViewModels.Nodes;

namespace WPFNode.Controls;

public abstract class PortControl : Control
{
    static PortControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PortControl),
            new FrameworkPropertyMetadata(typeof(PortControl)));
    }

    public NodePortViewModel? ViewModel
    {
        get => (NodePortViewModel?)DataContext;
        set => DataContext = value;
    }

    private Point? _dragStart;
    private Path? _dragPath;
    private Canvas? _dragCanvas;
    private Point? _portCenter;
    public bool IsDragging => _dragStart.HasValue;

    public static readonly RoutedEvent PortDragStartEvent = EventManager.RegisterRoutedEvent(
        nameof(PortDragStart), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PortControl));

    public static readonly RoutedEvent PortDragEndEvent = EventManager.RegisterRoutedEvent(
        nameof(PortDragEnd), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PortControl));

    public event RoutedEventHandler PortDragStart
    {
        add { AddHandler(PortDragStartEvent, value); }
        remove { RemoveHandler(PortDragStartEvent, value); }
    }

    public event RoutedEventHandler PortDragEnd
    {
        add { AddHandler(PortDragEndEvent, value); }
        remove { RemoveHandler(PortDragEndEvent, value); }
    }

    protected PortControl()
    {
        Background = Brushes.LightGray;
        BorderBrush = Brushes.DarkGray;

        MouseLeftButtonDown += OnPortMouseDown;
        MouseLeftButtonUp += OnPortMouseUp;
        MouseMove += OnPortMouseMove;
    }

    private Point GetPortEdgePosition(Point center, Point target)
    {
        const double radius = 6.0;
        var dx = target.X - center.X;
        var dy = target.Y - center.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        
        if (distance < 0.0001) return center;

        var x = center.X + (dx / distance) * radius;
        var y = center.Y + (dy / distance) * radius;
        
        return new Point(x, y);
    }

    private PathGeometry CreateBezierPathGeometry(Point start, Point end)
    {
        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure();

        // 시작점과 끝점을 각각의 Ellipse 가장자리로 조정
        var adjustedStart = GetPortEdgePosition(start, end);
        var adjustedEnd = GetPortEdgePosition(end, start);
        
        pathFigure.StartPoint = adjustedStart;

        // 제어점 계산 (시작점과 끝점의 x 차이를 이용하여 곡률 조정)
        var deltaX = Math.Abs(adjustedEnd.X - adjustedStart.X);
        var control1 = new Point(adjustedStart.X + deltaX * 0.5, adjustedStart.Y);
        var control2 = new Point(adjustedEnd.X - deltaX * 0.5, adjustedEnd.Y);

        var segment = new BezierSegment(control1, control2, adjustedEnd, true);
        pathFigure.Segments.Add(segment);
        pathGeometry.Figures.Add(pathFigure);

        return pathGeometry;
    }

    public Point GetConnectionPointCenter(UIElement relativeTo)
    {
        var connectionPoint = GetTemplateChild("PART_ConnectionPoint") as Ellipse;
        if (connectionPoint == null) return new Point();

        var isInput = ViewModel?.IsInput ?? false;
        var xOffset = isInput ? 6 : ActualWidth - 6; // 입력 포트는 왼쪽, 출력 포트는 오른쪽

        var ellipseCenter = new Point(xOffset, 6);
        return this.TranslatePoint(ellipseCenter, relativeTo);
    }

    private Point GetPortCenterPosition()
    {
        if (_dragCanvas == null) return new Point();
        return GetConnectionPointCenter(_dragCanvas);
    }

    private Point GetTargetPortPosition(FrameworkElement targetElement)
    {
        if (_dragCanvas == null) return new Point();

        var targetPort = targetElement as PortControl;
        if (targetPort?.ViewModel == null) return new Point();

        double xOffset = targetPort.ViewModel.IsInput ? 0 : 12;
        return targetPort.TranslatePoint(new Point(xOffset, 6), _dragCanvas);
    }

    private void OnPortMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel == null) return;

        _dragStart = e.GetPosition(this);
        _dragCanvas = this.GetParentOfType<NodeCanvasControl>()?.GetDragCanvas();

        if (_dragCanvas != null)
        {
            _dragPath = new Path
            {
                Stroke = Brushes.Gray,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection(new[] { 4d, 2d })
            };

            // 드래그 시작 시 포트 중심점 계산
            _portCenter = GetPortCenterPosition();
            var currentPos = e.GetPosition(_dragCanvas);

            if (ViewModel.IsInput)
            {
                _dragPath.Data = CreateBezierPathGeometry(currentPos, _portCenter.Value);
            }
            else
            {
                _dragPath.Data = CreateBezierPathGeometry(_portCenter.Value, currentPos);
            }

            _dragCanvas.Children.Add(_dragPath);
            RaiseEvent(new RoutedEventArgs(PortDragStartEvent));
        }

        CaptureMouse();
        e.Handled = true;
    }

    private void OnPortMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragStart.HasValue && ViewModel != null)
        {
            if (_dragCanvas != null)
            {
                var mousePosition = e.GetPosition(_dragCanvas);
                var hitTestResult = VisualTreeHelper.HitTest(_dragCanvas, mousePosition);
                if (hitTestResult?.VisualHit is FrameworkElement targetElement)
                {
                    var targetPort = targetElement.DataContext as NodePortViewModel;
                    if (targetPort != null && ViewModel.CanConnectTo(targetPort))
                    {
                        var canvas = this.GetParentOfType<NodeCanvasControl>();
                        if (canvas?.ViewModel != null)
                        {
                            if (ViewModel.IsInput)
                            {
                                canvas.ViewModel.ConnectCommand.Execute((targetPort, ViewModel));
                            }
                            else
                            {
                                canvas.ViewModel.ConnectCommand.Execute((ViewModel, targetPort));
                            }
                        }
                    }
                }
            }
        }

        if (_dragPath != null && _dragCanvas != null)
        {
            _dragCanvas.Children.Remove(_dragPath);
            _dragPath = null;
        }

        if (_dragStart.HasValue)
        {
            RaiseEvent(new RoutedEventArgs(PortDragEndEvent));
        }

        _dragStart = null;
        _portCenter = null;
        ReleaseMouseCapture();
    }

    private void OnPortMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStart.HasValue && ViewModel != null && IsMouseCaptured && _dragPath != null && _dragCanvas != null && _portCenter.HasValue)
        {
            var currentPos = e.GetPosition(_dragCanvas);

            if (ViewModel.IsInput)
            {
                _dragPath.Data = CreateBezierPathGeometry(currentPos, _portCenter.Value);
            }
            else
            {
                _dragPath.Data = CreateBezierPathGeometry(_portCenter.Value, currentPos);
            }
        }
    }

    private T? GetParentOfType<T>(DependencyObject? element) where T : DependencyObject
    {
        if (element == null) return null;
        if (element is T t) return t;
        return GetParentOfType<T>(VisualTreeHelper.GetParent(element));
    }
}

public class InputPortControl : PortControl
{
    static InputPortControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(InputPortControl),
            new FrameworkPropertyMetadata(typeof(InputPortControl)));
    }
}

public class OutputPortControl : PortControl
{
    static OutputPortControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(OutputPortControl),
            new FrameworkPropertyMetadata(typeof(OutputPortControl)));
    }
} 
