using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFNode.Core.ViewModels.Nodes;

namespace WPFNode.Controls;

public class ConnectionControl : Control
{
    private Path? _path;
    private PathGeometry? _lastPathGeometry;

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
        Foreground = Brushes.Gray;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _path = GetTemplateChild("PART_Path") as Path;
        UpdateConnectionPath();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"ConnectionControl OnLoaded: {ViewModel?.Source?.Name} -> {ViewModel?.Target?.Name}");
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas != null)
        {
            canvas.LayoutUpdated += OnCanvasLayoutUpdated;
            System.Diagnostics.Debug.WriteLine($"LayoutUpdated 이벤트 구독 성공");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"NodeCanvasControl을 찾을 수 없음");
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"ConnectionControl OnUnloaded: {ViewModel?.Source?.Name} -> {ViewModel?.Target?.Name}");
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas != null)
        {
            canvas.LayoutUpdated -= OnCanvasLayoutUpdated;
            System.Diagnostics.Debug.WriteLine($"LayoutUpdated 이벤트 구독 해제");
        }
    }

    private void OnCanvasLayoutUpdated(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"ConnectionControl LayoutUpdated: {ViewModel?.Source?.Name} -> {ViewModel?.Target?.Name}");
        UpdateConnectionPath();
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

    private void UpdateConnectionPath()
    {
        if (ViewModel == null) return;

        var sourcePort = GetPortControl(ViewModel.Source);
        var targetPort = GetPortControl(ViewModel.Target);

        if (sourcePort == null || targetPort == null) return;

        var sourceCenter = GetPortCenterPosition(sourcePort);
        var targetCenter = GetPortCenterPosition(targetPort);

        // Ellipse 가장자리 위치 계산
        var startPoint = GetPortEdgePosition(sourceCenter, targetCenter);
        var endPoint = GetPortEdgePosition(targetCenter, sourceCenter);

        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure { StartPoint = startPoint };

        // 제어점 계산 (시작점과 끝점의 x 차이를 이용하여 곡률 조정)
        var deltaX = Math.Abs(endPoint.X - startPoint.X);
        var control1 = new Point(startPoint.X + deltaX * 0.5, startPoint.Y);
        var control2 = new Point(endPoint.X - deltaX * 0.5, endPoint.Y);

        var segment = new BezierSegment(control1, control2, endPoint, true);
        pathFigure.Segments.Add(segment);
        pathGeometry.Figures.Add(pathFigure);

        // 이전 Geometry와 비교하여 변경이 있을 때만 업데이트
        if (_path != null && !GeometriesEqual(_lastPathGeometry, pathGeometry))
        {
            System.Diagnostics.Debug.WriteLine($"ConnectionControl Path 업데이트: {ViewModel?.Source?.Name} -> {ViewModel?.Target?.Name}");
            _path.Data = pathGeometry;
            _lastPathGeometry = pathGeometry;
        }
    }

    private bool GeometriesEqual(PathGeometry? g1, PathGeometry? g2)
    {
        if (g1 == null || g2 == null) return false;
        if (g1.Figures.Count != g2.Figures.Count) return false;

        for (int i = 0; i < g1.Figures.Count; i++)
        {
            var f1 = g1.Figures[i];
            var f2 = g2.Figures[i];

            if (f1.StartPoint != f2.StartPoint) return false;
            if (f1.Segments.Count != f2.Segments.Count) return false;

            for (int j = 0; j < f1.Segments.Count; j++)
            {
                if (f1.Segments[j] is BezierSegment b1 && f2.Segments[j] is BezierSegment b2)
                {
                    if (b1.Point1 != b2.Point1 || b1.Point2 != b2.Point2 || b1.Point3 != b2.Point3)
                        return false;
                }
                else return false;
            }
        }
        return true;
    }

    private PortControl? GetPortControl(NodePortViewModel port)
    {
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        return canvas?.FindPortControl(port);
    }
} 
