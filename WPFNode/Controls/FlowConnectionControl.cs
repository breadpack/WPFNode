using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFNode.Models;
using WPFNode.ViewModels.Nodes;

namespace WPFNode.Controls;

/// <summary>
/// 흐름 연결을 시각적으로 표현하는 컨트롤
/// </summary>
public class FlowConnectionControl : Control
{
    static FlowConnectionControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FlowConnectionControl), 
            new FrameworkPropertyMetadata(typeof(FlowConnectionControl)));
    }

    /// <summary>
    /// 생성자
    /// </summary>
    public FlowConnectionControl()
    {
        // 기본 스타일 설정
        Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 255));  // 푸른 계열 색상
        StrokeThickness = 2.5;
        
        // 흐름 연결은 점선으로 표시
        StrokeDashArray = new DoubleCollection { 4, 2 };
        
        // 이벤트 핸들러
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        
        // 데이터 컨텍스트 변경 이벤트
        DataContextChanged += OnDataContextChanged;
    }

    #region 의존성 속성

    /// <summary>
    /// 선 색상 속성
    /// </summary>
    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(FlowConnectionControl),
            new PropertyMetadata(Brushes.Blue));

    /// <summary>
    /// 선 두께 속성
    /// </summary>
    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(FlowConnectionControl),
            new PropertyMetadata(2.5));

    /// <summary>
    /// 선 패턴 속성
    /// </summary>
    public static readonly DependencyProperty StrokeDashArrayProperty =
        DependencyProperty.Register(nameof(StrokeDashArray), typeof(DoubleCollection), typeof(FlowConnectionControl),
            new PropertyMetadata(new DoubleCollection { 4, 2 }));

    /// <summary>
    /// 경로 기하학 속성
    /// </summary>
    public static readonly DependencyProperty PathGeometryProperty =
        DependencyProperty.Register(nameof(PathGeometry), typeof(Geometry), typeof(FlowConnectionControl),
            new PropertyMetadata(null));

    /// <summary>
    /// 선택 속성
    /// </summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(FlowConnectionControl),
            new PropertyMetadata(false));

    /// <summary>
    /// 강조 표시 속성
    /// </summary>
    public static readonly DependencyProperty IsHighlightedProperty =
        DependencyProperty.Register(nameof(IsHighlighted), typeof(bool), typeof(FlowConnectionControl),
            new PropertyMetadata(false));

    #endregion

    #region 속성

    /// <summary>
    /// 선 색상
    /// </summary>
    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>
    /// 선 두께
    /// </summary>
    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>
    /// 선 패턴
    /// </summary>
    public DoubleCollection StrokeDashArray
    {
        get => (DoubleCollection)GetValue(StrokeDashArrayProperty);
        set => SetValue(StrokeDashArrayProperty, value);
    }

    /// <summary>
    /// 경로 기하학
    /// </summary>
    public Geometry PathGeometry
    {
        get => (Geometry)GetValue(PathGeometryProperty);
        set => SetValue(PathGeometryProperty, value);
    }

    /// <summary>
    /// 선택 여부
    /// </summary>
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// 강조 표시 여부
    /// </summary>
    public bool IsHighlighted
    {
        get => (bool)GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }

    /// <summary>
    /// ViewModel 속성
    /// </summary>
    private FlowConnectionViewModel? ViewModel => DataContext as FlowConnectionViewModel;

    #endregion

    #region 이벤트 핸들러

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        IsHighlighted = true;
        if (ViewModel != null)
        {
            ViewModel.IsHighlighted = true;
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        IsHighlighted = false;
        if (ViewModel != null)
        {
            ViewModel.IsHighlighted = false;
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            // 단일 클릭으로 선택
            if (ViewModel != null)
            {
                ViewModel.IsSelected = !ViewModel.IsSelected;
                IsSelected = ViewModel.IsSelected;
            }
            
            e.Handled = true;
        }
        else if (e.ClickCount == 2)
        {
            // 더블 클릭으로 삭제
            if (ViewModel != null && ViewModel.Connection != null)
            {
                // 연결의 소스 쪽 노드를 통해 NodeCanvas를 찾아 연결 제거
                var sourceNode = ViewModel.Connection.Source.Node;
                if (sourceNode is NodeBase nodeBase && nodeBase.Canvas != null)
                {
                    if (nodeBase.Canvas is NodeCanvas canvas)
                    {
                        canvas.DisconnectFlow(ViewModel.Connection);
                    }
                }
            }
            
            e.Handled = true;
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is FlowConnectionViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        
        if (e.NewValue is FlowConnectionViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            
            // 초기 속성 설정
            UpdateFromViewModel(viewModel);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (ViewModel == null) return;
        
        switch (e.PropertyName)
        {
            case nameof(FlowConnectionViewModel.IsHighlighted):
                IsHighlighted = ViewModel.IsHighlighted;
                break;
            case nameof(FlowConnectionViewModel.IsSelected):
                IsSelected = ViewModel.IsSelected;
                break;
            case nameof(FlowConnectionViewModel.SourcePosition):
            case nameof(FlowConnectionViewModel.TargetPosition):
                UpdatePathGeometry();
                break;
        }
    }

    private void UpdateFromViewModel(FlowConnectionViewModel viewModel)
    {
        // 속성 설정
        IsSelected = viewModel.IsSelected;
        IsHighlighted = viewModel.IsHighlighted;
        
        // 경로 업데이트
        UpdatePathGeometry();
    }

    /// <summary>
    /// 연결선의 경로 기하학 업데이트
    /// </summary>
    private void UpdatePathGeometry()
    {
        if (ViewModel == null) return;
        
        var source = ViewModel.SourcePosition;
        var target = ViewModel.TargetPosition;
        
        // 시작 위치와 끝 위치의 중간 지점 계산
        double midX = (source.X + target.X) / 2;
        
        // 베지어 곡선용 제어점 계산 (흐름 연결을 다르게 표현하기 위해 데이터 포트 연결과 다른 곡선 사용)
        var controlPoint1 = new Point(midX, source.Y);
        var controlPoint2 = new Point(midX, target.Y);
        
        // 경로 생성
        PathFigure figure = new PathFigure
        {
            StartPoint = source,
            IsClosed = false
        };
        
        // 화살표 시작점
        figure.Segments.Add(new BezierSegment(controlPoint1, controlPoint2, target, true));
        
        // 화살표 추가
        double arrowSize = 8;
        double angle = Math.Atan2(target.Y - controlPoint2.Y, target.X - controlPoint2.X);
        
        Point arrowPoint1 = new Point(
            target.X - arrowSize * Math.Cos(angle - Math.PI / 6),
            target.Y - arrowSize * Math.Sin(angle - Math.PI / 6)
        );
        
        Point arrowPoint2 = new Point(
            target.X - arrowSize * Math.Cos(angle + Math.PI / 6),
            target.Y - arrowSize * Math.Sin(angle + Math.PI / 6)
        );
        
        PathFigure arrowFigure1 = new PathFigure
        {
            StartPoint = target,
            IsClosed = false
        };
        arrowFigure1.Segments.Add(new LineSegment(arrowPoint1, true));
        
        PathFigure arrowFigure2 = new PathFigure
        {
            StartPoint = target,
            IsClosed = false
        };
        arrowFigure2.Segments.Add(new LineSegment(arrowPoint2, true));
        
        // 전체 경로 생성
        PathGeometry = new PathGeometry();
        ((PathGeometry)PathGeometry).Figures.Add(figure);
        ((PathGeometry)PathGeometry).Figures.Add(arrowFigure1);
        ((PathGeometry)PathGeometry).Figures.Add(arrowFigure2);
    }

    #endregion
}
