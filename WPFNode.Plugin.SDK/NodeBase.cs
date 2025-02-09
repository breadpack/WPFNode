using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Reflection;
using WPFNode.Abstractions;

namespace WPFNode.Plugin.SDK;

public abstract class NodeBase : INode
{
    private string _name;
    private readonly string _category;
    private readonly string _description;
    private double _x;
    private double _y;
    private bool _isSelected;
    private bool _isProcessing;
    private bool _isVisible = true;
    private readonly List<IPort> _inputPorts = new();
    private readonly List<IPort> _outputPorts = new();

    protected NodeBase()
    {
        Id = Guid.NewGuid();

        // 메타데이터 초기화
        var metadata = GetNodeMetadata(GetType());
        _name = metadata.Name;
        _category = metadata.Category;
        _description = metadata.Description;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; internal set; }
    
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Category => _category;
    public string Description => _description;
    
    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }
    
    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public bool IsProcessing
    {
        get => _isProcessing;
        protected set => SetProperty(ref _isProcessing, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public IReadOnlyList<IPort> InputPorts => _inputPorts;
    public IReadOnlyList<IPort> OutputPorts => _outputPorts;

    public abstract Task ProcessAsync();

    protected void RegisterInputPort(IPort port)
    {
        if (!port.IsInput)
            throw new ArgumentException("입력 포트가 아닙니다.");
        _inputPorts.Add(port);
    }

    protected void RegisterOutputPort(IPort port)
    {
        if (port.IsInput)
            throw new ArgumentException("출력 포트가 아닙니다.");
        _outputPorts.Add(port);
    }

    public void Initialize()
    {
        InitializePorts();
    }

    protected virtual void InitializePorts()
    {
        // 파생 클래스에서 포트 초기화를 구현
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public virtual bool CanExecuteCommand(string commandName, object? parameter = null)
    {
        return false;
    }

    public virtual void ExecuteCommand(string commandName, object? parameter = null)
    {
        throw new NotSupportedException($"명령 {commandName}을(를) 처리할 수 없습니다.");
    }

    public virtual NodeBase CreateCopy(double offsetX = 20, double offsetY = 20)
    {
        var copy = (NodeBase)MemberwiseClone();
        copy.Id = Guid.NewGuid();
        copy.X += offsetX;
        copy.Y += offsetY;

        copy._inputPorts.Clear();
        copy._outputPorts.Clear();
        copy.Initialize();

        return copy;
    }

    // 메타데이터를 가져오는 정적 메서드
    public static NodeMetadata GetNodeMetadata(Type nodeType)
    {
        if (nodeType == null)
            throw new ArgumentNullException(nameof(nodeType));

        if (!typeof(NodeBase).IsAssignableFrom(nodeType))
            throw new ArgumentException($"타입이 NodeBase를 상속하지 않습니다: {nodeType.Name}");

        var nameAttr = nodeType.GetCustomAttribute<NodeNameAttribute>();
        var categoryAttr = nodeType.GetCustomAttribute<NodeCategoryAttribute>();
        var descriptionAttr = nodeType.GetCustomAttribute<NodeDescriptionAttribute>();
        
        return new NodeMetadata(
            nodeType,
            nameAttr?.Name ?? nodeType.Name,
            categoryAttr?.Category ?? "Basic",
            descriptionAttr?.Description ?? string.Empty);
    }
} 