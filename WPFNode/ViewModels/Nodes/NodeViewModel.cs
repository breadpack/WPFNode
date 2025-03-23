using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.ViewModels.Base;
using ICommand = System.Windows.Input.ICommand;
using System.Linq;

namespace WPFNode.ViewModels.Nodes;

public class NodeViewModel : ViewModelBase, INodeViewModel, ISelectable, IDisposable
{
    private readonly NodeBase _model;
    private readonly INodeCommandService _commandService;
    private readonly NodeCanvasViewModel _canvas;
    private Point _position;
    private string _name;

    private readonly ObservableCollection<NodePortViewModel> _inputPorts;
    private readonly ObservableCollection<NodePortViewModel> _outputPorts;
    private readonly ObservableCollection<NodePortViewModel> _flowInPorts;
    private readonly ObservableCollection<NodePortViewModel> _flowOutPorts;
    public ReadOnlyObservableCollection<NodePortViewModel> InputPorts { get; }
    public ReadOnlyObservableCollection<NodePortViewModel> OutputPorts { get; }
    public ReadOnlyObservableCollection<NodePortViewModel> FlowInPorts { get; }
    public ReadOnlyObservableCollection<NodePortViewModel> FlowOutPorts { get; }

    public NodeViewModel(NodeBase model, INodeCommandService commandService, NodeCanvasViewModel canvas)
    {
        _model          = model;
        _commandService = commandService;
        _canvas         = canvas;
        _position       = new(model.X, model.Y);
        _name           = model.Name;

        // 포트 컬렉션 초기화
        _inputPorts = new(model.InputPorts.Select(p => new NodePortViewModel(p, canvas)));
        _outputPorts = new(model.OutputPorts.Select(p => new NodePortViewModel(p, canvas)));
        _flowInPorts = new(model.FlowInPorts.Select(p => new NodePortViewModel(p, canvas)));
        _flowOutPorts = new(model.FlowOutPorts.Select(p => new NodePortViewModel(p, canvas)));

        InputPorts = new(_inputPorts);
        OutputPorts = new(_outputPorts);
        FlowInPorts = new(_flowInPorts);
        FlowOutPorts = new(_flowOutPorts);

        // 포트의 Parent 설정
        foreach (var port in InputPorts.Concat(OutputPorts).Concat(FlowInPorts).Concat(FlowOutPorts))
        {
            port.Parent = this;
        }

        // 포트 컬렉션 변경 이벤트 구독
        _inputPorts.CollectionChanged += OnPortsCollectionChanged;
        _outputPorts.CollectionChanged += OnPortsCollectionChanged;
        _flowInPorts.CollectionChanged += OnPortsCollectionChanged;
        _flowOutPorts.CollectionChanged += OnPortsCollectionChanged;

        // 모델 속성 변경 이벤트 구독
        _model.PropertyChanged += Model_PropertyChanged;

        // 선택 상태 변경 이벤트 구독
        _canvas.SelectedItems.CollectionChanged += SelectedItemsOnCollectionChanged;

        // 명령 초기화
        DeleteCommand = new RelayCommand(Delete);
    }

    private void SelectedItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        if (e is { Action: NotifyCollectionChangedAction.Add, NewItems: not null }) {
            if (e.NewItems.OfType<INodeViewModel>().Any(item => item == this)) {
                OnPropertyChanged(nameof(IsSelected));
            }
        } else if (e is { Action: NotifyCollectionChangedAction.Remove, OldItems: not null }) {
            if (e.OldItems.OfType<INodeViewModel>().Any(item => item == this)) {
                OnPropertyChanged(nameof(IsSelected));
            }
        }
        else if (e is { Action: NotifyCollectionChangedAction.Reset }) {
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public void Dispose() {
        _model.PropertyChanged -= Model_PropertyChanged;
        _canvas.SelectedItems.CollectionChanged -= SelectedItemsOnCollectionChanged;
    }

    public Guid Id => _model.Guid;
    
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

    public string Description
    {
        get => _model.Description;
        set => _model.Description = value;
    }

    public string Category => _model.Category;

    public bool IsVisible
    {
        get => _model.IsVisible;
        set => _model.IsVisible = value;
    }

    public IReadOnlyList<INodeProperty> Properties => _model.Properties;
    
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

    /// <summary>
    /// 노드가 선택되었는지 확인합니다.
    /// </summary>
    public bool IsSelected => _canvas.IsItemSelected(this);

    /// <summary>
    /// 노드를 선택합니다.
    /// </summary>
    /// <param name="clearOthers">다른 항목의 선택을 해제할지 여부</param>
    public void Select(bool clearOthers = true)
    {
        _canvas.SelectItem(this, clearOthers);
    }

    /// <summary>
    /// 노드의 선택을 해제합니다.
    /// </summary>
    public void Deselect()
    {
        _canvas.DeselectItem(this);
    }

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
        
        // 포트 컬렉션이 변경되면 캔버스에 알림
        _canvas.OnPortsChanged();
    }

    public bool ExecuteCommand(string commandName, object? parameter = null)
    {
        return _commandService.ExecuteCommand(_model.Guid, commandName, parameter);
    }

    public bool CanExecuteCommand(string commandName, object? parameter = null)
    {
        return _commandService.CanExecuteCommand(_model.Guid, commandName, parameter);
    }

    public ICommand? GetCommand(string commandName)
    {
        return new RelayCommand(
            () => ExecuteCommand(commandName),
            () => CanExecuteCommand(commandName));
    }

    public INode Model => _model;

    private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(INode.InputPorts):
                _inputPorts.Clear();
                foreach (var port in _model.InputPorts)
                {
                    _inputPorts.Add(new(port, _canvas));
                }
                break;
            case nameof(INode.OutputPorts):
                _outputPorts.Clear();
                foreach (var port in _model.OutputPorts)
                {
                    _outputPorts.Add(new(port, _canvas));
                }
                break;
            case nameof(NodeBase.FlowInPorts):
                _flowInPorts.Clear();
                foreach (var port in _model.FlowInPorts)
                {
                    _flowInPorts.Add(new(port, _canvas));
                }
                break;
            case nameof(NodeBase.FlowOutPorts):
                _flowOutPorts.Clear();
                foreach (var port in _model.FlowOutPorts)
                {
                    _flowOutPorts.Add(new(port, _canvas));
                }
                break;
            case nameof(INode.Properties):
                OnPropertyChanged(nameof(Properties));
                // Properties가 변경되면 InputPorts도 함께 갱신
                _inputPorts.Clear();
                foreach (var port in _model.InputPorts)
                {
                    _inputPorts.Add(new(port, _canvas));
                }
                break;
            case nameof(NodeBase.X):
            case nameof(NodeBase.Y):
                Position = new(_model.X, _model.Y);
                break;
            case nameof(NodeBase.Name):
                Name = _model.Name;
                break;
            case nameof(NodeBase.Description):
                OnPropertyChanged(nameof(Description));
                break;
            case nameof(NodeBase.IsVisible):
                OnPropertyChanged(nameof(IsVisible));
                break;
            case nameof(NodeBase.Category):
                OnPropertyChanged(nameof(Category));
                break;
        }
    }

    public (ReadOnlyObservableCollection<NodePortViewModel> InputPorts, 
            ReadOnlyObservableCollection<NodePortViewModel> OutputPorts,
            ReadOnlyObservableCollection<NodePortViewModel> FlowInPorts,
            ReadOnlyObservableCollection<NodePortViewModel> FlowOutPorts) GetPorts()
    {
        return (InputPorts, OutputPorts, FlowInPorts, FlowOutPorts);
    }

    public Type GetNodeType()
    {
        return Model.GetType();
    }

    // ISelectable 인터페이스 구현
    public string SelectionType => "Node";
}
