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

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ConnectionViewModel),
            typeof(ConnectionControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public ConnectionViewModel? ViewModel
    {
        get => (ConnectionViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ConnectionControl control)
        {
            control.DataContext = e.NewValue;
            control.UpdatePath();
        }
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
        UpdatePath();
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
        UpdatePath();
    }

    private void UpdatePath()
    {
        if (_path == null || ViewModel == null) return;

        var sourcePort = ViewModel.Source;
        var targetPort = ViewModel.Target;

        if (sourcePort == null || targetPort == null) return;

        var sourceControl = FindPortControl(sourcePort);
        var targetControl = FindPortControl(targetPort);

        if (sourceControl == null || targetControl == null) return;

        var sourcePoint = sourceControl.TranslatePoint(new Point(6, 6), this);
        var targetPoint = targetControl.TranslatePoint(new Point(6, 6), this);

        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = sourcePoint };

        // 베지어 곡선으로 연결선 그리기
        var controlPoint1 = new Point(sourcePoint.X + 50, sourcePoint.Y);
        var controlPoint2 = new Point(targetPoint.X - 50, targetPoint.Y);
        figure.Segments.Add(new BezierSegment(controlPoint1, controlPoint2, targetPoint, true));

        geometry.Figures.Add(figure);
        _path.Data = geometry;
    }

    private PortControl? FindPortControl(NodePortViewModel port)
    {
        var canvas = this.GetParentOfType<NodeCanvasControl>();
        if (canvas == null) return null;

        return canvas.GetVisualDescendants()
            .OfType<PortControl>()
            .FirstOrDefault(p => p.ViewModel == port);
    }
} 
