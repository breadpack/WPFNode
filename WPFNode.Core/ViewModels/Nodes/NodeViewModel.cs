using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WPFNode.Core.Models;
using WPFNode.Core.ViewModels.Base;
using CommunityToolkit.Mvvm.Input;
using WPFNode.Core.Interfaces;
using WPFNode.Core.Services;
using WPFNode.Plugin.SDK;
using System.Collections.Specialized;

namespace WPFNode.Core.ViewModels.Nodes;

public class NodeViewModel : ViewModelBase
{
    private readonly NodeBase                                    _model;
    private readonly INodeCommandService                     _commandService;
    private          Point                                   _position;
    private          string                                  _name;
    private          bool                                    _isSelected;
    private readonly ObservableCollection<NodePortViewModel> _inputPorts;
    private readonly ObservableCollection<NodePortViewModel> _outputPorts;

    public NodeViewModel(NodeBase model, INodeCommandService commandService, NodeCanvasViewModel canvas)
    {
        _model = model;
        _commandService = commandService;
        _name = model.Name;
        _position = new Point(model.X, model.Y);
        _isSelected = model.IsSelected;
        
        _inputPorts = new ObservableCollection<NodePortViewModel>(
            model.InputPorts.Select(p => new NodePortViewModel(p, canvas)));
        _outputPorts = new ObservableCollection<NodePortViewModel>(
            model.OutputPorts.Select(p => new NodePortViewModel(p, canvas)));

        // 포트의 Parent 설정
        foreach (var port in _inputPorts.Concat(_outputPorts))
        {
            port.Parent = this;
        }

        // 포트 컬렉션 변경 이벤트 구독
        _inputPorts.CollectionChanged += OnPortsCollectionChanged;
        _outputPorts.CollectionChanged += OnPortsCollectionChanged;

        DeleteCommand = new RelayCommand(Delete);
        
        // Model 속성 변경 감지
        _model.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(NodeBase.X):
                case nameof(NodeBase.Y):
                    Position = new Point(_model.X, _model.Y);
                    break;
                case nameof(NodeBase.Name):
                    Name = _model.Name;
                    break;
                case nameof(NodeBase.IsSelected):
                    IsSelected = _model.IsSelected;
                    break;
            }
        };
    }

    public Guid Id => _model.Id;
    
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                _model.Name = value;
            }
        }
    }
    
    public Point Position
    {
        get => _position;
        set
        {
            if (SetProperty(ref _position, value))
            {
                _model.X = value.X;
                _model.Y = value.Y;
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                _model.IsSelected = value;
            }
        }
    }

    public IReadOnlyList<NodePortViewModel> InputPorts => _inputPorts;
    public IReadOnlyList<NodePortViewModel> OutputPorts => _outputPorts;

    public ICommand DeleteCommand { get; }

    private void Delete()
    {
        // 삭제 로직은 NodeCanvasViewModel에서 처리
    }

    private void OnPortsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (NodePortViewModel port in e.NewItems)
            {
                port.Parent = this;
            }
        }
    }

    // 노드 타입별 커맨드 실행
    public bool ExecuteCommand(string commandName, object? parameter = null)
    {
        return _commandService.ExecuteCommand(_model.Id, commandName, parameter);
    }

    public bool CanExecuteCommand(string commandName, object? parameter = null)
    {
        return _commandService.CanExecuteCommand(_model.Id, commandName, parameter);
    }

    // 노드 타입별 커맨드 조회
    public ICommand? GetCommand(string commandName)
    {
        return new RelayCommand(
            () => ExecuteCommand(commandName),
            () => CanExecuteCommand(commandName));
    }

    public NodeBase Model => _model;
} 