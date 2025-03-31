using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFNode.Utilities;
using WPFNode.ViewModels.Nodes;
using System.Linq;
using System.Collections.Generic;

namespace WPFNode.Controls;

public abstract class PortControl : Control
{
    private Point? _dragStart;
    private Path? _dragPath;
    private Canvas? _dragCanvas;
    private Point? _portCenter;
    private readonly Brush _originalBackground;
    private bool _isHighlighted;
    private PortControl? _lastTargetPort;
    
    private static readonly Brush ValidConnectionBrush = Brushes.LightGreen;
    private static readonly Brush InvalidConnectionBrush = Brushes.Red;
    private static readonly Brush DefaultConnectionBrush = Brushes.Gray;
    
    private static readonly DoubleCollection DashedStroke = new(new[] { 4d, 2d });
    private const double ConnectionLineThickness = 2.0;
    private const double ConnectionPointRadius = 6.0;

    private static readonly DependencyPropertyKey IsInputPropertyKey = 
        DependencyProperty.RegisterReadOnly(
            nameof(IsInput),
            typeof(bool),
            typeof(PortControl),
            new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IsInputProperty = IsInputPropertyKey.DependencyProperty;

    public bool IsInput
    {
        get => (bool)GetValue(IsInputProperty);
        protected set => SetValue(IsInputPropertyKey, value);
    }
    
    public bool IsDragging => _dragStart.HasValue;

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

    public PortControl()
    {
        _originalBackground = Background ?? Brushes.Transparent;
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        // 다른 포트에서 드래그 중일 때만 연결 가능 여부 표시
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas?.IsDraggingPort == true && canvas.DraggingPort != null && ViewModel != null)
        {
            var draggingPort = canvas.DraggingPort;
            if (draggingPort.CanConnectTo(ViewModel))
            {
                Background = Brushes.LightGreen;
                _isHighlighted = true;
            }
            else
            {
                Background = Brushes.Red;
                _isHighlighted = true;
            }
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (_isHighlighted)
        {
            Background = _originalBackground;
            _isHighlighted = false;
        }
    }

    public Point GetConnectionPointCenter(UIElement relativeTo)
    {
        var xOffset = IsInput ? 6 : ActualWidth - 6;
        var yOffset = ActualHeight / 2;

        return TranslatePoint(new Point(xOffset, yOffset), relativeTo);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        
        if (ViewModel == null) return;

        _dragStart = e.GetPosition(this);
        _portCenter = GetConnectionPointCenter(this);
        
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas != null)
        {
            canvas.StartPortDrag(ViewModel);
            _dragCanvas = canvas.GetDragCanvas();
            if (_dragCanvas != null)
            {
                _dragPath = new Path
                {
                    Stroke = DefaultConnectionBrush,
                    StrokeThickness = ConnectionLineThickness,
                    StrokeDashArray = DashedStroke,
                    IsHitTestVisible = false // 드래그 중인 라인이 히트 테스트를 방해하지 않도록 함
                };
                _dragCanvas.Children.Add(_dragPath);
            }
            CaptureMouse();
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_dragPath != null && _dragCanvas != null && _dragStart.HasValue && _portCenter.HasValue)
        {
            var currentPos = e.GetPosition(_dragCanvas);
            var portPos = GetConnectionPointCenter(_dragCanvas);

            PortControl? targetPort = null;
            const double hitTolerance = 5.0; // Pixels around the cursor - adjust as needed
            Rect hitArea = new Rect(currentPos.X - hitTolerance, currentPos.Y - hitTolerance,
                                    hitTolerance * 2, hitTolerance * 2);
            RectangleGeometry hitGeometry = new RectangleGeometry(hitArea);
            GeometryHitTestParameters parameters = new GeometryHitTestParameters(hitGeometry);

            HitTestResultCallback hitTestCallback = result =>
            {
                if (result.VisualHit is DependencyObject hitVisual)
                {
                    var potentialTarget = hitVisual as PortControl ?? hitVisual.GetParentOfType<PortControl>();
                    
                    if (potentialTarget != null && potentialTarget != this)
                    {
                        targetPort = potentialTarget;
                        return HitTestResultBehavior.Stop;
                    }
                }
                return HitTestResultBehavior.Continue;
            };

            VisualTreeHelper.HitTest(_dragCanvas, null, hitTestCallback, parameters);

            _lastTargetPort = targetPort;

            UpdateConnectionLineStyle(_dragPath, targetPort);

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = portPos };

            // 제어점 계산 (시작점과 끝점의 x 차이를 이용하여 곡률 조정)
            var deltaX = Math.Abs(currentPos.X - portPos.X);
            Point control1, control2;

            if (IsInput)
            {
                // 입력 포트: 왼쪽에서 오른쪽으로
                control1 = new Point(portPos.X - deltaX * 0.5, portPos.Y);
                control2 = new Point(currentPos.X + deltaX * 0.5, currentPos.Y);
            }
            else
            {
                // 출력 포트: 오른쪽에서 왼쪽으로
                control1 = new Point(portPos.X + deltaX * 0.5, portPos.Y);
                control2 = new Point(currentPos.X - deltaX * 0.5, currentPos.Y);
            }

            var segment = new BezierSegment(control1, control2, currentPos, true);
            pathFigure.Segments.Add(segment);
            pathGeometry.Figures.Add(pathFigure);

            _dragPath.Data = pathGeometry;
        }
    }

    private void UpdateConnectionLineStyle(Path path, PortControl? targetPort)
    {
        if (targetPort == null || targetPort == this || ViewModel == null || targetPort.ViewModel == null)
        {
            // 유효한 대상이 없을 때는 기본 스타일
            path.Stroke = DefaultConnectionBrush;
            path.StrokeThickness = ConnectionLineThickness;
            path.StrokeDashArray = DashedStroke;
            return;
        }

        if (ViewModel.CanConnectTo(targetPort.ViewModel))
        {
            // 연결 가능한 경우
            path.Stroke = ValidConnectionBrush;
            path.StrokeThickness = ConnectionLineThickness * 1.5; // 더 두껍게
            path.StrokeDashArray = null; // 실선
        }
        else
        {
            // 연결 불가능한 경우
            path.Stroke = InvalidConnectionBrush;
            path.StrokeThickness = ConnectionLineThickness;
            path.StrokeDashArray = DashedStroke;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_dragPath != null && _dragCanvas != null && 
            _lastTargetPort?.ViewModel != null && ViewModel != null &&
            ViewModel.CanConnectTo(_lastTargetPort.ViewModel))
        {
            var nodeCanvas = this.GetParentOfType<NodeCanvasControl>();
            if (nodeCanvas?.ViewModel != null)
            {
                if (ViewModel.IsInput)
                {
                    nodeCanvas.ViewModel.ConnectCommand.Execute((_lastTargetPort.ViewModel, ViewModel));
                }
                else
                {
                    nodeCanvas.ViewModel.ConnectCommand.Execute((ViewModel, _lastTargetPort.ViewModel));
                }
            }

            _dragCanvas.Children.Remove(_dragPath);
            _dragPath = null;
        }
        else if (_dragPath != null && _dragCanvas != null)
        {
            _dragCanvas.Children.Remove(_dragPath);
            _dragPath = null;
        }

        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas != null)
        {
            canvas.EndPortDrag();
        }

        ReleaseMouseCapture();
        _dragStart = null;
        _lastTargetPort = null;
        Background = _originalBackground;
        _isHighlighted = false;
    }

    private T? GetParentOfType<T>(DependencyObject? element) where T : DependencyObject
    {
        if (element == null) return null;
        if (element is T t) return t;
        return GetParentOfType<T>(VisualTreeHelper.GetParent(element));
    }
}