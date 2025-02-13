using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using WPFNode.Core.Models;
using WPFNode.Core.ViewModels.Base;
using WPFNode.Core.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using WPFNode.Core.Interfaces;
using WPFNode.Abstractions;
using WPFNode.Core.Commands;
using IWpfCommand = System.Windows.Input.ICommand;
using System.Collections.Specialized;
using System.Windows;

namespace WPFNode.Core.ViewModels.Nodes;

public partial class NodeCanvasViewModel : ObservableObject
{
    private readonly NodeCanvas _canvas;
    private readonly INodePluginService _pluginService;
    private readonly INodeCommandService _commandService;
    private readonly WPFNode.Core.Commands.CommandManager _commandManager;

    public ObservableCollection<NodeViewModel> Nodes { get; }
    public ObservableCollection<ConnectionViewModel> Connections { get; }
    public ObservableCollection<NodeGroupViewModel> Groups { get; }

    [ObservableProperty]
    private double _scale = 1.0;

    [ObservableProperty]
    private double _offsetX;

    [ObservableProperty]
    private double _offsetY;

    public IWpfCommand AddNodeCommand { get; }
    public IWpfCommand RemoveNodeCommand { get; }
    public IWpfCommand ConnectCommand { get; }
    public IWpfCommand DisconnectCommand { get; }
    public IWpfCommand AddGroupCommand { get; }
    public IWpfCommand RemoveGroupCommand { get; }
    public IWpfCommand UndoCommand { get; }
    public IWpfCommand RedoCommand { get; }
    public IWpfCommand ExecuteCommand { get; }
    public IWpfCommand CopyCommand { get; }
    public IWpfCommand PasteCommand { get; }

    public NodeCanvasViewModel(
        NodeCanvas canvas,
        INodePluginService pluginService,
        INodeCommandService commandService)
    {
        _canvas = canvas;
        _pluginService = pluginService;
        _commandService = commandService;
        _commandManager = new WPFNode.Core.Commands.CommandManager();
        
        Nodes = new ObservableCollection<NodeViewModel>(
            canvas.Nodes.Select(n => CreateNodeViewModel(n)));
        
        Connections = new ObservableCollection<ConnectionViewModel>(
            canvas.Connections.Select(c => new ConnectionViewModel(c, this)));
        Groups = new ObservableCollection<NodeGroupViewModel>();

        AddNodeCommand = new RelayCommand<Type>(ExecuteAddNode);
        RemoveNodeCommand = new RelayCommand<NodeViewModel>(ExecuteRemoveNode);
        ConnectCommand = new RelayCommand<(NodePortViewModel, NodePortViewModel)>(ExecuteConnect);
        DisconnectCommand = new RelayCommand<(NodePortViewModel, NodePortViewModel)>(ExecuteDisconnect);
        AddGroupCommand = new RelayCommand<NodeGroup>(AddGroup);
        RemoveGroupCommand = new RelayCommand<NodeGroupViewModel>(RemoveGroup);
        UndoCommand = new RelayCommand(ExecuteUndo, CanExecuteUndo);
        RedoCommand = new RelayCommand(ExecuteRedo, CanExecuteRedo);
        ExecuteCommand = new RelayCommand(ExecuteNodes);
        CopyCommand = new RelayCommand(CopySelectedNodes);
        PasteCommand = new RelayCommand(PasteNodes);

        // Model 변경 감지를 위한 이벤트 핸들러 등록
        SynchronizeWithModel();
    }

    private void SynchronizeWithModel()
    {
        // 노드 동기화
        Nodes.Clear();
        foreach (var node in _canvas.Nodes)
        {
            Nodes.Add(CreateNodeViewModel(node));
        }

        // 연결 동기화
        Connections.Clear();
        foreach (var connection in _canvas.Connections)
        {
            Connections.Add(new ConnectionViewModel(connection, this));
        }

        // 그룹 동기화
        Groups.Clear();
        foreach (var group in _canvas.Groups)
        {
            Groups.Add(new NodeGroupViewModel(group, this));
        }
    }

    private NodeViewModel CreateNodeViewModel(INode node)
    {
        if (node is not NodeBase nodeBase)
            throw new ArgumentException("노드는 NodeBase 타입이어야 합니다.");
            
        return new NodeViewModel(nodeBase, _commandService, this);
    }

    private void ExecuteAddNode(Type? nodeType)
    {
        if (nodeType == null) return;
        
        var command = new AddNodeCommand(_canvas, nodeType);
        _commandManager.Execute(command);
        
        SynchronizeWithModel();
    }

    private void ExecuteRemoveNode(NodeViewModel? nodeViewModel)
    {
        if (nodeViewModel == null) return;
        
        var command = new RemoveNodeCommand(_canvas, nodeViewModel.Model);
        _commandManager.Execute(command);
        
        SynchronizeWithModel();
    }

    private bool IsValidConnection(NodePortViewModel sourcePort, NodePortViewModel targetPort)
    {
        if (sourcePort == null || targetPort == null) return false;

        // 같은 노드의 포트인 경우 연결 불가
        var sourceNode = Nodes.FirstOrDefault(n => 
            n.InputPorts.Contains(sourcePort) || n.OutputPorts.Contains(sourcePort));
        var targetNode = Nodes.FirstOrDefault(n => 
            n.InputPorts.Contains(targetPort) || n.OutputPorts.Contains(targetPort));
            
        if (sourceNode == null || targetNode == null || sourceNode == targetNode)
            return false;

        // 입력-출력 포트 방향이 맞는지 확인
        if (sourcePort.IsInput == targetPort.IsInput)
            return false;

        // 이미 연결된 포트인지 확인
        if (Connections.Any(c => 
            (c.Source == sourcePort && c.Target == targetPort) ||
            (c.Source == targetPort && c.Target == sourcePort)))
            return false;

        // 포트 타입 호환성 확인
        return sourcePort.CanConnectTo(targetPort);
    }

    private void ExecuteConnect((NodePortViewModel source, NodePortViewModel target) ports)
    {
        if (!IsValidConnection(ports.source, ports.target))
            return;

        var (source, target) = ports;
        
        // 입력 포트에 기존 연결이 있는지 확인하고 제거
        var inputPort = source.IsInput ? source : target;
        var existingConnection = inputPort.Connections.FirstOrDefault()?.Model;
        if (existingConnection != null)
        {
            _canvas.Disconnect(existingConnection);
        }

        // 새 연결 생성
        var connection = _canvas.Connect(source.Model, target.Model);
        if (connection != null)
        {
            SynchronizeWithModel();
        }
    }

    private void ExecuteDisconnect((NodePortViewModel source, NodePortViewModel target) ports)
    {
        var connection = Connections.FirstOrDefault(c =>
            (c.Source == ports.source && c.Target == ports.target) ||
            (c.Source == ports.target && c.Target == ports.source))?.Model;

        if (connection != null)
        {
            _canvas.Disconnect(connection);
            SynchronizeWithModel();
        }
    }

    private void ExecuteUndo()
    {
        _commandManager.Undo();
        SynchronizeWithModel();
    }

    private void ExecuteRedo()
    {
        _commandManager.Redo();
        SynchronizeWithModel();
    }

    private bool CanExecuteUndo() => _commandManager.CanUndo;
    private bool CanExecuteRedo() => _commandManager.CanRedo;

    private void AddGroup(NodeGroup? group)
    {
        if (group == null) return;
        
        var selectedNodes = Nodes
            .Where(n => n.IsSelected)
            .Select(n => (NodeBase)n.Model)
            .ToList();
            
        if (selectedNodes.Any())
        {
            var command = new AddGroupCommand(_canvas, group.Name, selectedNodes);
            _commandManager.Execute(command);
            SynchronizeWithModel();
        }
    }

    private void RemoveGroup(NodeGroupViewModel? groupVM)
    {
        if (groupVM?.Model == null) return;
        
        var command = new RemoveGroupCommand(_canvas, groupVM.Model);
        _commandManager.Execute(command);
        SynchronizeWithModel();
    }

    public NodePortViewModel FindPortViewModel(IPort port)
    {
        foreach (var node in Nodes)
        {
            var foundPort = node.InputPorts.FirstOrDefault(p => p.Model == port) ??
                           node.OutputPorts.FirstOrDefault(p => p.Model == port);
            if (foundPort != null)
                return foundPort;
        }
        return null;
    }

    public NodeCanvas Model => _canvas;

    private async void ExecuteNodes()
    {
        try
        {
            await _canvas.ExecuteAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"노드 실행 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopySelectedNodes()
    {
        var selectedNodes = Nodes.Where(n => n.IsSelected).ToList();
        if (selectedNodes.Any())
        {
            var nodeDataList = selectedNodes.Select(n => ((NodeBase)n.Model).CreateCopy()).ToList();
            System.Windows.Clipboard.SetDataObject(nodeDataList);
        }
    }

    private void PasteNodes()
    {
        var dataObject = System.Windows.Clipboard.GetDataObject();
        if (dataObject?.GetData(typeof(List<NodeBase>)) is List<NodeBase> nodeDataList)
        {
            foreach (var node in nodeDataList)
            {
                var newNode = _canvas.CreateNode(node.GetType(), node.X + 20, node.Y + 20);
                // 필요한 속성들을 복사
                if (newNode is NodeBase nodeBase)
                {
                    nodeBase.Name = node.Name;
                }
            }
            SynchronizeWithModel();
        }
    }
} 