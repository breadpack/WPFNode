using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFNode.Core.Models;
using WPFNode.Core.Services;
using System.Linq;
using System.Collections.Generic;
using WPFNode.Core.ViewModels.Nodes;

namespace WPFNode.Controls;

public class NodeCanvasControl : Control
{
    private Point? _lastMousePosition;
    private NodePortViewModel? _dragStartPort;
    private Line? _dragLine;
    private Canvas? _dragCanvas;
    private SearchPanel? _searchPanel;
    private readonly NodeTemplateService _templateService;

    static NodeCanvasControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeCanvasControl),
            new FrameworkPropertyMetadata(typeof(NodeCanvasControl)));
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(NodeCanvasViewModel),
            typeof(NodeCanvasControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public NodeCanvasViewModel? ViewModel
    {
        get => (NodeCanvasViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty NameProperty =
        DependencyProperty.Register(
            nameof(Name),
            typeof(string),
            typeof(NodeCanvasControl),
            new PropertyMetadata(null));

    public string? Name
    {
        get => (string?)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NodeCanvasControl control)
        {
            control.DataContext = e.NewValue;
            System.Diagnostics.Debug.WriteLine($"NodeCanvasControl ViewModel changed: {e.NewValue}");
            if (e.NewValue is NodeCanvasViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine($"Current node count: {viewModel.Nodes.Count}");
            }
        }
    }

    public NodeCanvasControl()
    {
        _templateService = new NodeTemplateService();
        Background = Brushes.LightGray;
        
        // 마우스 이벤트 처리
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;

        // 키보드 이벤트 처리
        Focusable = true;
        KeyDown += OnKeyDown;

        // 크기 설정
        Width = 800;
        Height = 600;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        // 디버깅용 메시지
        System.Diagnostics.Debug.WriteLine($"NodeCanvasControl created: {Width}x{Height}");
    }

    public NodeCanvasControl(NodeTemplateService templateService)
        : this()
    {
        _templateService = templateService;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _dragCanvas = GetTemplateChild("PART_Canvas") as Canvas;
        _searchPanel = GetTemplateChild("PART_SearchPanel") as SearchPanel;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _lastMousePosition = e.GetPosition(this);
        Focus();
        CaptureMouse();

        // 포트 클릭 확인
        if (e.Source is PortControl portControl && portControl.ViewModel != null)
        {
            _dragStartPort = portControl.ViewModel;
            _dragLine = new Line
            {
                Stroke = Brushes.Gray,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection(new[] { 4d, 2d })
            };

            if (_dragCanvas != null)
            {
                _dragCanvas.Children.Add(_dragLine);
                var portPosition = portControl.TranslatePoint(new Point(6, 6), _dragCanvas);
                if (_dragStartPort.IsInput)
                {
                    _dragLine.X2 = portPosition.X;
                    _dragLine.Y2 = portPosition.Y;
                }
                else
                {
                    _dragLine.X1 = portPosition.X;
                    _dragLine.Y1 = portPosition.Y;
                }
            }
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragStartPort != null && e.Source is PortControl portControl && portControl.ViewModel != null)
        {
            var endPort = portControl.ViewModel;
            if (_dragStartPort.IsInput)
            {
                ViewModel?.ConnectCommand.Execute((endPort, _dragStartPort));
            }
            else
            {
                ViewModel?.ConnectCommand.Execute((_dragStartPort, endPort));
            }
        }

        if (_dragLine != null && _dragCanvas != null)
        {
            _dragCanvas.Children.Remove(_dragLine);
            _dragLine = null;
        }

        _dragStartPort = null;
        _lastMousePosition = null;
        ReleaseMouseCapture();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _lastMousePosition.HasValue && ViewModel != null)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMousePosition.Value;
            
            ViewModel.OffsetX += delta.X;
            ViewModel.OffsetY += delta.Y;
            
            _lastMousePosition = currentPosition;
        }
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ViewModel == null) return;

        var zoomCenter = e.GetPosition(this);
        var delta = e.Delta * 0.001;
        var newScale = ViewModel.Scale + delta;
        newScale = Math.Max(0.1, Math.Min(2.0, newScale));

        // 줌 중심점 기준으로 스케일 조정
        var dx = zoomCenter.X - ViewModel.OffsetX;
        var dy = zoomCenter.Y - ViewModel.OffsetY;
        
        ViewModel.OffsetX = zoomCenter.X - dx * (newScale / ViewModel.Scale);
        ViewModel.OffsetY = zoomCenter.Y - dy * (newScale / ViewModel.Scale);
        ViewModel.Scale = newScale;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel == null) return;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.Space:
                    ShowSearchPanel();
                    e.Handled = true;
                    break;

                case Key.C:
                    CopySelectedNodes();
                    e.Handled = true;
                    break;

                case Key.V:
                    PasteNodes();
                    e.Handled = true;
                    break;

                case Key.A:
                    SelectAllNodes();
                    e.Handled = true;
                    break;
            }
        }
        else if (e.Key == Key.Delete)
        {
            DeleteSelectedNodes();
            e.Handled = true;
        }
    }

    private void SelectAllNodes()
    {
        if (ViewModel == null) return;
        foreach (var node in ViewModel.Nodes)
        {
            node.IsSelected = true;
        }
    }

    private void CopySelectedNodes()
    {
        if (ViewModel == null) return;
        var selectedNodes = ViewModel.Nodes.Where(n => n.IsSelected).ToList();
        if (selectedNodes.Any())
        {
            var nodeDataList = selectedNodes.Select(n => n.Model.Clone()).ToList();
            System.Windows.Clipboard.SetDataObject(nodeDataList);
        }
    }

    private void PasteNodes()
    {
        if (ViewModel == null) return;
        var dataObject = System.Windows.Clipboard.GetDataObject();
        if (dataObject?.GetData(typeof(List<Node>)) is List<Node> nodeDataList)
        {
            foreach (var node in nodeDataList)
            {
                ViewModel.AddNodeCommand.Execute(node);
            }
        }
    }

    private void DeleteSelectedNodes()
    {
        if (ViewModel == null) return;
        var selectedNodes = ViewModel.Nodes.Where(n => n.IsSelected).ToList();
        foreach (var node in selectedNodes)
        {
            ViewModel.RemoveNodeCommand.Execute(node);
        }
    }

    private void ShowSearchPanel()
    {
        if (_searchPanel != null && ViewModel != null)
        {
            _searchPanel.Visibility = Visibility.Visible;
            _searchPanel.Focus();
        }
    }
} 
