using System;
using System.Windows;
using System.Windows.Media;
using WPFNode.Core.Models;
using WPFNode.Core.ViewModels.Nodes;
using System.Linq;
using WPFNode.Core.Services;
using WPFNode.Controls;

namespace WPFNode.Demo
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _nodeCounter = 1;
        private readonly NodeCanvasViewModel _viewModel;
        private NodeCanvasControl? _nodeCanvas;
        private FrameworkElement? _mainGrid;

        public MainWindow()
        {
            InitializeComponent();
            
            // ViewModel 초기화
            var canvas = new NodeCanvas();
            var templateService = new NodeTemplateService();
            _viewModel = new NodeCanvasViewModel(canvas, templateService);
            
            // 초기 상태 설정
            _viewModel.Scale = 1.0;
            _viewModel.OffsetX = 0;
            _viewModel.OffsetY = 0;
            
            // 컨트롤 찾기
            _nodeCanvas = this.FindName("NodeCanvas") as NodeCanvasControl;
            _mainGrid = this.FindName("MainGrid") as FrameworkElement;
            
            // NodeCanvas에 ViewModel 설정
            if (_nodeCanvas != null)
            {
                System.Diagnostics.Debug.WriteLine("Setting ViewModel to NodeCanvas");
                _nodeCanvas.ViewModel = _viewModel;
                this.DataContext = _viewModel;
                System.Diagnostics.Debug.WriteLine($"NodeCanvas DataContext: {_nodeCanvas.DataContext}");
                System.Diagnostics.Debug.WriteLine($"NodeCanvas ViewModel: {_nodeCanvas.ViewModel}");
            }

            // 디버깅용 메시지
            System.Diagnostics.Debug.WriteLine("NodeCanvas initialized");
            
            // 레이아웃 업데이트 후 크기 출력
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_nodeCanvas != null)
            {
                System.Diagnostics.Debug.WriteLine($"NodeCanvas size: {_nodeCanvas.ActualWidth}x{_nodeCanvas.ActualHeight}");
            }
            if (_mainGrid != null)
            {
                System.Diagnostics.Debug.WriteLine($"MainGrid size: {_mainGrid.ActualWidth}x{_mainGrid.ActualHeight}");
            }
        }

        private void AddNode_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("AddNode_Click called");
            var node = new Node(Guid.NewGuid().ToString(), $"노드 {_nodeCounter++}");
            
            // 기본 포트 추가
            node.InputPorts.Add(new NodePort(Guid.NewGuid().ToString(), "입력 1", typeof(object), true, node));
            node.OutputPorts.Add(new NodePort(Guid.NewGuid().ToString(), "출력 1", typeof(object), false, node));
            
            // 노드 위치 설정
            var random = new Random();
            var width = _nodeCanvas?.ActualWidth > 0 ? _nodeCanvas.ActualWidth : 800;
            var height = _nodeCanvas?.ActualHeight > 0 ? _nodeCanvas.ActualHeight : 600;
            node.X = random.Next(100, (int)width - 200);
            node.Y = random.Next(100, (int)height - 200);

            System.Diagnostics.Debug.WriteLine($"Adding node at position: ({node.X}, {node.Y})");
            _viewModel.AddNodeCommand.Execute(node);
            System.Diagnostics.Debug.WriteLine($"Current node count: {_viewModel.Nodes.Count}");
        }

        private void CreateGroup_Click(object sender, RoutedEventArgs e)
        {
            var selectedNodes = _viewModel.Nodes.Where(n => n.IsSelected).ToList();
            if (selectedNodes.Any())
            {
                var group = new NodeGroup(Guid.NewGuid().ToString(), "새 그룹");
                _viewModel.AddGroupCommand.Execute(group);
            }
        }

        private void AutoLayout_Click(object sender, RoutedEventArgs e)
        {
            // 노드들을 격자 형태로 자동 정렬
            var nodes = _viewModel.Nodes.ToList();
            var columns = (int)Math.Ceiling(Math.Sqrt(nodes.Count));
            var spacing = 150.0;

            for (int i = 0; i < nodes.Count; i++)
            {
                var row = i / columns;
                var col = i % columns;
                nodes[i].Model.X = col * spacing + 100;
                nodes[i].Model.Y = row * spacing + 100;
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UndoCommand.Execute(null);
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RedoCommand.Execute(null);
        }
    }
} 
