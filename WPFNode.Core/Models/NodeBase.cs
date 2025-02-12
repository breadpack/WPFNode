using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using WPFNode.Abstractions;
using WPFNode.Core.Attributes;

namespace WPFNode.Core.Models;

public abstract class NodeBase : INode, INotifyPropertyChanged
{
    private string _name;
    private readonly string _category;
    private readonly string _description;
    private double _x;
    private double _y;
    private bool _isProcessing;
    private bool _isVisible = true;
    private readonly List<IPort> _inputPorts = new();
    private readonly List<IPort> _outputPorts = new();

    protected NodeBase()
    {
        Id = Guid.NewGuid();

        // 어트리뷰트에서 직접 값을 가져옴
        var type = GetType();
        var nameAttr = type.GetCustomAttribute<NodeNameAttribute>();
        var categoryAttr = type.GetCustomAttribute<NodeCategoryAttribute>();
        var descriptionAttr = type.GetCustomAttribute<NodeDescriptionAttribute>();

        _name = nameAttr?.Name ?? type.Name;
        _category = categoryAttr?.Category ?? "Basic";
        _description = descriptionAttr?.Description ?? string.Empty;
    }

    [JsonPropertyName("id")]
    public Guid Id { get; internal set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Category => _category;
    public string Description => _description;
    
    [JsonPropertyName("x")]
    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }
    
    [JsonPropertyName("y")]
    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
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

    public bool IsOutputNode => GetType().GetCustomAttribute<OutputNodeAttribute>() != null;

    [JsonPropertyName("inputPorts")]
    public IReadOnlyList<IPort> InputPorts => _inputPorts;

    [JsonPropertyName("outputPorts")]
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

    protected abstract void InitializePorts();

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
} 