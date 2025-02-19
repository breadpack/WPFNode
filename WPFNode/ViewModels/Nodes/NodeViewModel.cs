using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.ViewModels.Base;
using ICommand = System.Windows.Input.ICommand;

namespace WPFNode.ViewModels.Nodes;

public class NodeViewModel : ViewModelBase
{
    private readonly NodeBase _model;
    private readonly INodeCommandService _commandService;
    private readonly NodeCanvasViewModel _canvas;
    private Point _position;
    private string _name;
    private bool _isSelected;

    public ObservableCollection<NodePortViewModel> InputPorts { get; }
    public ObservableCollection<NodePortViewModel> OutputPorts { get; }

    public NodeViewModel(NodeBase model, INodeCommandService commandService, NodeCanvasViewModel canvas)
    {
        _model = model;
        _commandService = commandService;
        _canvas = canvas;
        _name = model.Name;
        _position = new Point(model.X, model.Y);
        
        InputPorts = new ObservableCollection<NodePortViewModel>(
            model.InputPorts.Select(p => new NodePortViewModel(p, canvas)));
        OutputPorts = new ObservableCollection<NodePortViewModel>(
            model.OutputPorts.Select(p => new NodePortViewModel(p, canvas)));

        // 포트의 Parent 설정
        foreach (var port in InputPorts.Concat(OutputPorts))
        {
            port.Parent = this;
        }

        // 포트 컬렉션 변경 이벤트 구독
        InputPorts.CollectionChanged += OnPortsCollectionChanged;
        OutputPorts.CollectionChanged += OnPortsCollectionChanged;

        DeleteCommand = new RelayCommand(Delete);
        
        // Model 속성 변경 감지
        _model.PropertyChanged += Model_PropertyChanged;
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

    public IReadOnlyDictionary<string, INodeProperty> Properties => _model.Properties;
    
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
        set => SetProperty(ref _isSelected, value);
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
        return _commandService.ExecuteCommand(_model.Id, commandName, parameter);
    }

    public bool CanExecuteCommand(string commandName, object? parameter = null)
    {
        return _commandService.CanExecuteCommand(_model.Id, commandName, parameter);
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
                InputPorts.Clear();
                foreach (var port in _model.InputPorts)
                {
                    InputPorts.Add(new NodePortViewModel(port, _canvas));
                }
                break;
            case nameof(INode.OutputPorts):
                OutputPorts.Clear();
                foreach (var port in _model.OutputPorts)
                {
                    OutputPorts.Add(new NodePortViewModel(port, _canvas));
                }
                break;
            case nameof(INode.Properties):
                OnPropertyChanged(nameof(Properties));
                // Properties가 변경되면 InputPorts도 함께 갱신
                InputPorts.Clear();
                foreach (var port in _model.InputPorts)
                {
                    InputPorts.Add(new NodePortViewModel(port, _canvas));
                }
                break;
            case nameof(NodeBase.X):
            case nameof(NodeBase.Y):
                Position = new Point(_model.X, _model.Y);
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
} 