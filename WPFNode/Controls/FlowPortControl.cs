using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFNode.ViewModels.Nodes;

namespace WPFNode.Controls;

/// <summary>
/// 흐름 포트를 표시하는 컨트롤
/// </summary>
public class FlowPortControl : Control
{
    static FlowPortControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(FlowPortControl), 
            new FrameworkPropertyMetadata(typeof(FlowPortControl)));
    }

    /// <summary>
    /// 포트 컨트롤 생성자
    /// </summary>
    public FlowPortControl()
    {
        // 기본 속성 설정
        Fill = Brushes.DeepSkyBlue;
        BorderBrush = Brushes.DarkBlue;
        BorderThickness = 1;
        Foreground = Brushes.Black;
        
        // 이벤트 핸들러 등록
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseMove += OnMouseMove;
        
        // 데이터 변경 이벤트
        DataContextChanged += OnDataContextChanged;
    }

    #region 의존성 속성

    /// <summary>
    /// 도형 채우기 색상 속성
    /// </summary>
    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(FlowPortControl),
            new PropertyMetadata(Brushes.DeepSkyBlue));

    /// <summary>
    /// 도형 테두리 두께 속성
    /// </summary>
    public static readonly DependencyProperty BorderThicknessProperty =
        DependencyProperty.Register(nameof(BorderThickness), typeof(double), typeof(FlowPortControl),
            new PropertyMetadata(1.0));

    /// <summary>
    /// 도형 회전 각도 속성
    /// </summary>
    public static readonly DependencyProperty RotationAngleProperty =
        DependencyProperty.Register(nameof(RotationAngle), typeof(double), typeof(FlowPortControl),
            new PropertyMetadata(0.0));

    /// <summary>
    /// 텍스트 정렬 속성
    /// </summary>
    public static readonly DependencyProperty TextAlignmentProperty =
        DependencyProperty.Register(nameof(TextAlignment), typeof(HorizontalAlignment), typeof(FlowPortControl),
            new PropertyMetadata(HorizontalAlignment.Left));

    /// <summary>
    /// 텍스트 여백 속성
    /// </summary>
    public static readonly DependencyProperty TextMarginProperty =
        DependencyProperty.Register(nameof(TextMargin), typeof(Thickness), typeof(FlowPortControl),
            new PropertyMetadata(new Thickness(18, 0, 0, 0)));

    /// <summary>
    /// 강조 표시 속성
    /// </summary>
    public static readonly DependencyProperty IsHighlightedProperty =
        DependencyProperty.Register(nameof(IsHighlighted), typeof(bool), typeof(FlowPortControl),
            new PropertyMetadata(false));

    /// <summary>
    /// 연결 중 속성
    /// </summary>
    public static readonly DependencyProperty IsConnectingProperty =
        DependencyProperty.Register(nameof(IsConnecting), typeof(bool), typeof(FlowPortControl),
            new PropertyMetadata(false));

    /// <summary>
    /// 연결됨 속성
    /// </summary>
    public static readonly DependencyProperty IsConnectedProperty =
        DependencyProperty.Register(nameof(IsConnected), typeof(bool), typeof(FlowPortControl),
            new PropertyMetadata(false));

    #endregion

    #region 속성

    /// <summary>
    /// 도형 채우기 색상
    /// </summary>
    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>
    /// 도형 테두리 두께
    /// </summary>
    public double BorderThickness
    {
        get => (double)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    /// <summary>
    /// 도형 회전 각도
    /// </summary>
    public double RotationAngle
    {
        get => (double)GetValue(RotationAngleProperty);
        set => SetValue(RotationAngleProperty, value);
    }

    /// <summary>
    /// 텍스트 정렬
    /// </summary>
    public HorizontalAlignment TextAlignment
    {
        get => (HorizontalAlignment)GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    /// <summary>
    /// 텍스트 여백
    /// </summary>
    public Thickness TextMargin
    {
        get => (Thickness)GetValue(TextMarginProperty);
        set => SetValue(TextMarginProperty, value);
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
    /// 연결 중 여부
    /// </summary>
    public bool IsConnecting
    {
        get => (bool)GetValue(IsConnectingProperty);
        set => SetValue(IsConnectingProperty, value);
    }

    /// <summary>
    /// 연결됨 여부
    /// </summary>
    public bool IsConnected
    {
        get => (bool)GetValue(IsConnectedProperty);
        set => SetValue(IsConnectedProperty, value);
    }

    /// <summary>
    /// ViewModel 속성
    /// </summary>
    private FlowPortViewModel? ViewModel => DataContext as FlowPortViewModel;

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
        // 이벤트 캡처
        CaptureMouse();
        
        if (ViewModel != null)
        {
            ViewModel.IsConnecting = true;
            IsConnecting = true;
            
            // 연결 드래그 중으로 상태 변경
            // 이 상태에서는 MouseMove 이벤트를 통해 시각적 업데이트
            // 실제 연결 과정은 NodeViewModel을 통해 처리
            // (현재는 UI 기반 구현만 폼)
        }
        
        e.Handled = true;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsConnecting)
        {
            ReleaseMouseCapture();
            
            if (ViewModel != null)
            {
                ViewModel.IsConnecting = false;
                IsConnecting = false;
                
                // UI 연결 처리는 추후 구현 예정
                // 현재는 ViewModel만 상태 변경
            }
        }
        
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (IsConnecting && e.LeftButton == MouseButtonState.Pressed)
        {
            // UI 드래그 시각 효과는 추후 구현 예정
            // 현재 빌드에서는 이 부분을 처리하는 UI 요소가 미구현
            e.Handled = true;
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is FlowPortViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        
        if (e.NewValue is FlowPortViewModel viewModel)
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
            case nameof(FlowPortViewModel.IsHighlighted):
                IsHighlighted = ViewModel.IsHighlighted;
                break;
            case nameof(FlowPortViewModel.IsConnecting):
                IsConnecting = ViewModel.IsConnecting;
                break;
            case nameof(FlowPortViewModel.IsConnected):
                IsConnected = ViewModel.IsConnected;
                break;
            case nameof(FlowPortViewModel.Position):
                // 위치 관련 업데이트가 필요하면 처리
                break;
        }
    }

    private void UpdateFromViewModel(FlowPortViewModel viewModel)
    {
        // 입력/출력 포트에 따라 회전 각도 및 텍스트 정렬 설정
        if (viewModel.IsInput)
        {
            RotationAngle = 180;  // 입력 포트는 왼쪽을 가리킴
            TextAlignment = HorizontalAlignment.Left;
            TextMargin = new Thickness(18, 0, 0, 0);
        }
        else
        {
            RotationAngle = 0;    // 출력 포트는 오른쪽을 가리킴
            TextAlignment = HorizontalAlignment.Right;
            TextMargin = new Thickness(0, 0, 18, 0);
        }
        
        // 기타 속성 설정
        IsConnected = viewModel.IsConnected;
        IsConnecting = viewModel.IsConnecting;
        IsHighlighted = viewModel.IsHighlighted;
        
        // 흐름 포트용 색상 설정 (데이터 포트와 다르게)
        Fill = new SolidColorBrush(Color.FromRgb(128, 128, 255));  // 연한 파란색
        BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 192));  // 진한 파란색
    }

    #endregion
}

/// <summary>
/// 비주얼 트리 확장 메서드
/// </summary>
public static class VisualTreeExtensions
{
    /// <summary>
    /// 비주얼 트리에서 특정 타입의 부모를 찾는 메서드
    /// </summary>
    public static T? FindVisualParent<T>(this DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        
        if (parent == null)
            return null;
            
        if (parent is T typedParent)
            return typedParent;
            
        return FindVisualParent<T>(parent);
    }
}
