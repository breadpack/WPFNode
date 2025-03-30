using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.ComponentModel;
using WPFNode.Controls;
using WPFNode.Models;
using WPFNode.ViewModels.Nodes;
using WPFNode.Demo.Models;
using WPFNode.Demo.ViewModels;
using WPFNode.ViewModels;

namespace WPFNode.Demo
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly NodeCanvasControl _nodeCanvasControl;
        private readonly MainWindowViewModel _viewModel;

        public NodeCanvas NodeCanvas => (DataContext as MainWindowViewModel)?.NodeCanvasViewModel?.Model;

        public MainWindow()
        {
            InitializeComponent();
            _nodeCanvasControl = (NodeCanvasControl)FindName("NodeCanvasControl");
            _viewModel = DataContext as MainWindowViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            
            if (_nodeCanvasControl != null)
            {
                // 노드 관련 이벤트
                _nodeCanvasControl.NodeAdded += OnNodeAdded;
                _nodeCanvasControl.NodeRemoved += OnNodeRemoved;
                _nodeCanvasControl.NodeMoved += OnNodeMoved;
                _nodeCanvasControl.NodeSelected += OnNodeSelected;
                _nodeCanvasControl.NodeDeselected += OnNodeDeselected;

                // 연결 관련 이벤트
                _nodeCanvasControl.ConnectionAdded += OnConnectionAdded;
                _nodeCanvasControl.ConnectionRemoved += OnConnectionRemoved;

                // 뷰포트 변경 이벤트
                _nodeCanvasControl.ViewportChanged += OnViewportChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsMigrationCompleted))
            {
                if (_viewModel.IsMigrationCompleted)
                {
                    UpdateResultDataGrid();
                }
            }
        }

        private void UpdateResultDataGrid()
        {
            // 결과 데이터그리드 찾기
            var resultDataGrid = FindName("ResultDataGrid") as DataGrid;
            if (resultDataGrid == null) return;

            // 기존 컬럼 제거
            resultDataGrid.Columns.Clear();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SaveCanvas();
            }
        }

        private void OnNodeAdded(object sender, NodeViewModel node)
        {
            LogEvent($"노드 추가됨: {node.Model.Id}, 타입: {node.Model.GetType().Name}");
        }

        private void OnNodeRemoved(object sender, NodeViewModel node)
        {
            LogEvent($"노드 제거됨: {node.Model.Id}");
        }

        private void OnNodeMoved(object sender, NodeViewModel node)
        {
            LogEvent($"노드 이동됨: {node.Model.Id}, 위치: ({node.Model.X:F2}, {node.Model.Y:F2})");
        }

        private void OnNodeSelected(object sender, NodeViewModel node)
        {
            LogEvent($"노드 선택됨: {node.Model.Id}");
        }

        private void OnNodeDeselected(object sender, NodeViewModel node)
        {
            LogEvent($"노드 선택 해제됨: {node.Model.Id}");
        }

        private void OnConnectionAdded(object sender, ConnectionViewModel connection)
        {
            LogEvent($"연결 추가됨: {connection.Model.Guid}, " +
                    $"시작: {connection.Model.Source.Node.Id}, " +
                    $"끝: {connection.Model.Target.Node.Id}");
        }

        private void OnConnectionRemoved(object sender, ConnectionViewModel connection)
        {
            LogEvent($"연결 제거됨: {connection.Model.Guid}");
        }

        private void OnViewportChanged(object sender, ViewportChangedEventArgs e)
        {
            LogEvent($"뷰포트 변경됨: 스케일={e.Scale:F2}, 오프셋=({e.OffsetX:F2}, {e.OffsetY:F2})");
        }

        private void LogEvent(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[NodeCanvas Event] {message}");
        }
    }
} 
