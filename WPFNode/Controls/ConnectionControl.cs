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
    
    // 데이터 연결 스타일
    private static readonly Brush DefaultBrush = Brushes.Gray;
    private static readonly Brush SelectedBrush = Brushes.Orange;
    private const double DefaultThickness = 2.0;
    private const double SelectedThickness = 3.0;
    
    // Flow 연결 스타일 - 더 진한 연두색으로 변경
    private static readonly Brush FlowDefaultBrush = new SolidColorBrush(Color.FromRgb(122, 183, 48)); // 진한 연두색
    private static readonly Brush FlowSelectedBrush = new SolidColorBrush(Color.FromRgb(92, 159, 35)); // 선택 시 더 진한 연두색
    private static readonly Brush FlowOutlineBrush = new SolidColorBrush(Color.FromArgb(64, 40, 80, 20)); // 반투명 테두리 색상
    private const double FlowDefaultThickness = 4.5; // 두께 증가
    private const double FlowSelectedThickness = 5.5; // 선택 시 두께 증가
    private const double FlowOutlineThickness = 7.0; // 테두리 두께

    static ConnectionControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ConnectionControl),
            new FrameworkPropertyMetadata(typeof(ConnectionControl)));
    }

    public ConnectionViewModel? ViewModel {
        get => (ConnectionViewModel?)DataContext;
        set => DataContext = value;
    }

    // 현재 연결이 Flow 포트 간 연결인지 확인
    private bool IsFlowConnection => ViewModel?.Source?.IsFlow == true && ViewModel?.Target?.IsFlow == true;

    public ConnectionControl()
    {
        Foreground = DefaultBrush;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        DataContextChanged += OnDataContextChanged;
        Panel.SetZIndex(this, 2); // 연결선이 노드 뒤에 표시되도록 Z-Index 설정
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
        
        // Flow 연결인지 확인
        bool isFlow = IsFlowConnection;
        
        if (ViewModel.IsSelected)
        {
            // 선택된 상태
            _path.Stroke = isFlow ? FlowSelectedBrush : SelectedBrush;
            _path.StrokeThickness = isFlow ? FlowSelectedThickness : SelectedThickness;
            
            if (_arrow != null)
            {
                _arrow.Fill = isFlow ? FlowSelectedBrush : SelectedBrush;
                _arrow.Stroke = isFlow ? FlowSelectedBrush : SelectedBrush;
            }
        }
        else
        {
            // 기본 상태
            _path.Stroke = isFlow ? FlowDefaultBrush : DefaultBrush;
            _path.StrokeThickness = isFlow ? FlowDefaultThickness : DefaultThickness;
            
            if (_arrow != null)
            {
                _arrow.Fill = isFlow ? FlowDefaultBrush : DefaultBrush;
                _arrow.Stroke = isFlow ? FlowDefaultBrush : DefaultBrush;
            }
        }
        
        // Flow 연결은 실선으로 표시하되 테두리 추가
        if (isFlow)
        {
            // 주요 경로에 테두리 추가
            _path.StrokeDashArray = null;
            
            // 화살표도 두껍게 표시
            if (_arrow != null)
            {
                _arrow.StrokeThickness = 2.0;
            }
            
            // Panel.SetZIndex 설정
            Panel.SetZIndex(this, 2);
        }
        else
        {
            _path.StrokeDashArray = null;
            Panel.SetZIndex(this, 2);
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

        // 지연 호출로 변경 - Visual Tree가 완전히 구성된 후 실행되도록 함
        Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdateConnection();
            UpdateVisualState();
        }), System.Windows.Threading.DispatcherPriority.ContextIdle);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        
        // 지연 호출로 변경
        Dispatcher.BeginInvoke(new Action(UpdateConnection), 
            System.Windows.Threading.DispatcherPriority.ContextIdle);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"ConnectionControl OnLoaded: {ViewModel?.Source?.Name} -> {ViewModel?.Target?.Name}");
        
        // 로드 후에도 지연 호출 추가
        Dispatcher.BeginInvoke(new Action(UpdateConnection), 
            System.Windows.Threading.DispatcherPriority.ContextIdle);
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

    // 최적화를 위한 캐싱 변수
    private PortControl? _cachedSourcePort;
    private PortControl? _cachedTargetPort;
    private NodePortViewModel? _lastSource;
    private NodePortViewModel? _lastTarget;
    private bool _pendingUpdate;

    internal void UpdateConnection()
    {
        if (ViewModel == null)
            return;

        // 템플릿에서 필요한 요소를 찾지 못했다면 지연 호출로 다시 시도
        if (_pathFigure == null || _bezierSegment == null || _arrow == null)
        {
            if (!_pendingUpdate)
            {
                _pendingUpdate = true;
                Dispatcher.BeginInvoke(new Action(() => {
                    try 
                    {
                        // 템플릿 적용을 기다렸다가 다시 시도
                        _path = GetTemplateChild("PART_Path") as Path;
                        _arrow = GetTemplateChild("PART_Arrow") as Path;
                        _pathFigure = GetTemplateChild("PART_PathFigure") as PathFigure;
                        _bezierSegment = GetTemplateChild("PART_BezierSegment") as BezierSegment;
                        
                        if (_pathFigure != null && _bezierSegment != null && _arrow != null)
                        {
                            UpdateConnection();
                            UpdateVisualState(); // 시각 스타일도 업데이트
                        }
                    }
                    finally 
                    {
                        _pendingUpdate = false;
                    }
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
            return;
        }

        // 포트 컨트롤 가져오기 - 캐싱 적용
        PortControl? sourcePort;
        PortControl? targetPort;

        // Source 포트가 변경되었는지 확인
        if (_lastSource != ViewModel.Source)
        {
            sourcePort = GetPortControl(ViewModel.Source);
            _lastSource = ViewModel.Source;
            _cachedSourcePort = sourcePort;
        }
        else
        {
            sourcePort = _cachedSourcePort;
        }

        // Target 포트가 변경되었는지 확인
        if (_lastTarget != ViewModel.Target)
        {
            targetPort = GetPortControl(ViewModel.Target);
            _lastTarget = ViewModel.Target;
            _cachedTargetPort = targetPort;
        }
        else
        {
            targetPort = _cachedTargetPort;
        }

        // 포트를 찾지 못했으면 다시 시도
        if (sourcePort == null || targetPort == null)
        {
            if (!_pendingUpdate)
            {
                _pendingUpdate = true;
                Dispatcher.BeginInvoke(new Action(() => {
                    try 
                    {
                        // 캐시 초기화
                        _lastSource = null;
                        _lastTarget = null;
                        _cachedSourcePort = null;
                        _cachedTargetPort = null;
                        
                        UpdateConnection();
                    }
                    finally 
                    {
                        _pendingUpdate = false;
                    }
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
            return;
        }

        // 좌표 계산
        var sourceCenter = GetPortCenterPosition(sourcePort);
        var targetCenter = GetPortCenterPosition(targetPort);
            
        // Ellipse 가장자리 위치 계산
        var startPoint = GetPortEdgePosition(sourceCenter, targetCenter);
        var endPoint = GetPortEdgePosition(targetCenter, sourceCenter);

        // Flow 연결일 경우 곡선 제어점 계산 방식 조정
        bool isFlow = IsFlowConnection;
        
        // 베지어 곡선의 제어점 계산
        var deltaX = Math.Abs(endPoint.X - startPoint.X);
        
        // Flow 연결은 더 직선에 가깝게 표현
        var controlOffset = isFlow ? 0.2 : 0.5;
        var controlPoint1 = new Point(startPoint.X + deltaX * controlOffset, startPoint.Y);
        var controlPoint2 = new Point(endPoint.X - deltaX * controlOffset, endPoint.Y);

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
        
        // 시각적 스타일 업데이트
        UpdateVisualState();
    }
}
