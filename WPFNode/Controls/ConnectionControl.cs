using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFNode.Utilities;
using WPFNode.ViewModels.Nodes;

namespace WPFNode.Controls;

public class ConnectionControl : Control
{
    private Path? _path;
    private Path? _arrow;
    private PathFigure? _pathFigure;
    private BezierSegment? _bezierSegment;
    
    private static readonly Brush DefaultBrush = Brushes.Gray;
    private static readonly Brush SelectedBrush = Brushes.Orange;
    private const double DefaultThickness = 2.0;
    private const double SelectedThickness = 3.0;

    static ConnectionControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ConnectionControl),
            new FrameworkPropertyMetadata(typeof(ConnectionControl)));
    }

    public ConnectionViewModel? ViewModel {
        get => (ConnectionViewModel?)DataContext;
        set => DataContext = value;
    }

    public ConnectionControl()
    {
        Foreground = DefaultBrush;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ConnectionViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        
        if (e.NewValue is ConnectionViewModel newViewModel)
        {
            newViewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateVisualState();
        }
    }
    
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConnectionViewModel.IsSelected))
        {
            UpdateVisualState();
        }
    }
    
    private void UpdateVisualState()
    {
        if (ViewModel == null || _path == null) return;
        
        if (ViewModel.IsSelected)
        {
            _path.Stroke = SelectedBrush;
            _path.StrokeThickness = SelectedThickness;
            if (_arrow != null)
            {
                _arrow.Fill = SelectedBrush;
            }
        }
        else
        {
            _path.Stroke = DefaultBrush;
            _path.StrokeThickness = DefaultThickness;
            if (_arrow != null)
            {
                _arrow.Fill = DefaultBrush;
            }
        }
    }
    
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel == null) return;
        
        // 현재 연결선 선택 상태 토글
        if (ViewModel.IsSelected)
            ViewModel.Deselect();
        else
            ViewModel.Select();
        
        e.Handled = true;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _path = GetTemplateChild("PART_Path") as Path;
        _arrow = GetTemplateChild("PART_Arrow") as Path;
        _pathFigure = GetTemplateChild("PART_PathFigure") as PathFigure;
        _bezierSegment = GetTemplateChild("PART_BezierSegment") as BezierSegment;

        UpdateConnection();
        UpdateVisualState();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        UpdateConnection();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"ConnectionControl OnLoaded: {ViewModel?.Source?.Name} -> {ViewModel?.Target?.Name}");
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"ConnectionControl OnUnloaded: {ViewModel?.Source?.Name} -> {ViewModel?.Target?.Name}");
    }

    private Point GetPortCenterPosition(PortControl port)
    {
        return port.GetConnectionPointCenter(this);
    }

    private Point GetPortEdgePosition(Point center, Point target)
    {
        const double radius = 6.0;
        var dx = target.X - center.X;
        var dy = target.Y - center.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        
        if (distance < 0.0001) return center;

        // 중심점에서 target 방향으로 반지름만큼 이동한 점
        var x = center.X + (dx / distance) * radius;
        var y = center.Y + (dy / distance) * radius;
        
        return new Point(x, y);
    }

    private PortControl? GetPortControl(NodePortViewModel port)
    {
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        return canvas?.FindPortControl(port);
    }

    internal void UpdateConnection()
    {
        if (ViewModel == null || _pathFigure == null || _bezierSegment == null || _arrow == null)
            return;

        var sourcePort = GetPortControl(ViewModel.Source);
        var targetPort = GetPortControl(ViewModel.Target);

        if (sourcePort == null || targetPort == null) 
            return;

        var sourceCenter = GetPortCenterPosition(sourcePort);
        var targetCenter = GetPortCenterPosition(targetPort);
            
        // Ellipse 가장자리 위치 계산
        var startPoint = GetPortEdgePosition(sourceCenter, targetCenter);
        var endPoint = GetPortEdgePosition(targetCenter, sourceCenter);

        // 베지어 곡선의 제어점 계산
        var deltaX = Math.Abs(endPoint.X - startPoint.X);
        var controlPoint1 = new Point(startPoint.X + deltaX * 0.5, startPoint.Y);
        var controlPoint2 = new Point(endPoint.X - deltaX * 0.5, endPoint.Y);

        // 연결선 업데이트
        _pathFigure.StartPoint = startPoint;
        _bezierSegment.Point1 = controlPoint1;
        _bezierSegment.Point2 = controlPoint2;
        _bezierSegment.Point3 = endPoint;

        // 화살표 위치와 각도 계산
        var arrowPosition = endPoint;
        var direction = new Vector(
            controlPoint2.X - endPoint.X,
            controlPoint2.Y - endPoint.Y);
                
        direction.Normalize();
        var angle = Math.Atan2(direction.Y, direction.X) * 180 / Math.PI;

        // 화살표 변환 설정
        var arrowTransform = new TransformGroup();
        arrowTransform.Children.Add(new TranslateTransform(arrowPosition.X, arrowPosition.Y));
        arrowTransform.Children.Add(new RotateTransform(angle + 180));
        _arrow.RenderTransform = arrowTransform;
    }
} 
