using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFNode.Core.ViewModels.Nodes;

namespace WPFNode.Controls;

public class ConnectionControl : Control
{
    private Path? _path;

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
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas != null)
        {
            canvas.LayoutUpdated += OnCanvasLayoutUpdated;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas != null)
        {
            canvas.LayoutUpdated -= OnCanvasLayoutUpdated;
        }
    }

    private void OnCanvasLayoutUpdated(object? sender, EventArgs e)
    {
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

        if (GetTemplateChild("PART_Path") is Path path)
        {
            path.Data = pathGeometry;
        }
    }

    private PortControl? GetPortControl(NodePortViewModel port)
    {
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        return canvas?.FindPortControl(port);
    }
} 
