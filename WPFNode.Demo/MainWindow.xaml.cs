using System.Windows;
using WPFNode.Demo.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using WPFNode.Controls;
using WPFNode.ViewModels.Nodes;

namespace WPFNode.Demo
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly NodeCanvasControl _nodeCanvas;

        public MainWindow()
        {
            InitializeComponent();
            _nodeCanvas = (NodeCanvasControl)FindName("NodeCanvas");
            
            if (_nodeCanvas != null)
            {
                // 노드 관련 이벤트
                _nodeCanvas.NodeAdded += OnNodeAdded;
                _nodeCanvas.NodeRemoved += OnNodeRemoved;
                _nodeCanvas.NodeMoved += OnNodeMoved;
                _nodeCanvas.NodeSelected += OnNodeSelected;
                _nodeCanvas.NodeDeselected += OnNodeDeselected;

                // 연결 관련 이벤트
                _nodeCanvas.ConnectionAdded += OnConnectionAdded;
                _nodeCanvas.ConnectionRemoved += OnConnectionRemoved;

                // 뷰포트 변경 이벤트
                _nodeCanvas.ViewportChanged += OnViewportChanged;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SaveCanvas();
            }
        }

        private void OnNodeAdded(object? sender, NodeViewModel node)
        {
            LogEvent($"노드 추가됨: {node.Model.Id}, 타입: {node.Model.GetType().Name}");
        }

        private void OnNodeRemoved(object? sender, NodeViewModel node)
        {
            LogEvent($"노드 제거됨: {node.Model.Id}");
        }

        private void OnNodeMoved(object? sender, NodeViewModel node)
        {
            LogEvent($"노드 이동됨: {node.Model.Id}, 위치: ({node.Model.X:F2}, {node.Model.Y:F2})");
        }

        private void OnNodeSelected(object? sender, NodeViewModel node)
        {
            LogEvent($"노드 선택됨: {node.Model.Id}");
        }

        private void OnNodeDeselected(object? sender, NodeViewModel node)
        {
            LogEvent($"노드 선택 해제됨: {node.Model.Id}");
        }

        private void OnConnectionAdded(object? sender, ConnectionViewModel connection)
        {
            LogEvent($"연결 추가됨: {connection.Model.Id}, " +
                    $"시작: {connection.Model.Source.Node.Id}, " +
                    $"끝: {connection.Model.Target.Node.Id}");
        }

        private void OnConnectionRemoved(object? sender, ConnectionViewModel connection)
        {
            LogEvent($"연결 제거됨: {connection.Model.Id}");
        }

        private void OnViewportChanged(object? sender, ViewportChangedEventArgs e)
        {
            LogEvent($"뷰포트 변경됨: 스케일={e.Scale:F2}, 오프셋=({e.OffsetX:F2}, {e.OffsetY:F2})");
        }

        private void LogEvent(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[NodeCanvas Event] {message}");
        }
    }
} 
