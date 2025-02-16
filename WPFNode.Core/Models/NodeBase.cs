using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WPFNode.Abstractions;
using WPFNode.Abstractions.Attributes;
using WPFNode.Abstractions.Constants;
using WPFNode.Core.Models.Properties;

namespace WPFNode.Core.Models;

public abstract class NodeBase : INode, INotifyPropertyChanged
{
    private string _name = string.Empty;
    private readonly string _category;
    private string _description = string.Empty;
    private double _x;
    private double _y;
    private bool _isProcessing;
    private bool _isVisible = true;
    private readonly List<IInputPort> _inputPorts = new();
    private readonly List<IOutputPort> _outputPorts = new();
    private readonly Dictionary<string, INodeProperty> _properties = new();
    private bool _isInitialized;
    private readonly INodeCanvas _canvas;

    protected NodeBase(INodeCanvas canvas)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; internal set; }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public virtual string Category => _category;
    
    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }
    
    public double X
    {
        get => _x;
        set => SetField(ref _x, value);
    }
    
    public double Y
    {
        get => _y;
        set => SetField(ref _y, value);
    }
    
    public bool IsProcessing
    {
        get => _isProcessing;
        protected set => SetField(ref _isProcessing, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
    }

    public bool IsOutputNode => GetType().GetCustomAttribute<OutputNodeAttribute>() != null;

    public IReadOnlyList<IInputPort> InputPorts => _inputPorts;
    public IReadOnlyList<IOutputPort> OutputPorts => _outputPorts;
    public IReadOnlyDictionary<string, INodeProperty> Properties => _properties;

    public bool IsInitialized => _isInitialized;

    internal INodeCanvas Canvas => _canvas;

    public virtual void Initialize()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
    }

    protected void AddProperty<T>(
        string name,
        string displayName,
        NodePropertyControlType controlType,
        Func<T> getValue,
        Action<T> setValue,
        string? format = null,
        bool canConnectToPort = false)
    {
        _properties[name] = new NodeProperty<T>(
            displayName,
            controlType,
            getValue,
            setValue,
            format,
            canConnectToPort);
    }

    protected void RemoveProperty(string name)
    {
        _properties.Remove(name);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
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

    protected InputPort<T> CreateInputPort<T>(string name)
    {
        var port = new InputPort<T>(name, this);
        RegisterInputPort(port);
        return port;
    }

    protected OutputPort<T> CreateOutputPort<T>(string name)
    {
        var port = new OutputPort<T>(name, this);
        RegisterOutputPort(port);
        return port;
    }

    private void RegisterInputPort(IInputPort port)
    {
        if (port == null)
            throw new ArgumentNullException(nameof(port));
        _inputPorts.Add(port);
    }

    private void RegisterOutputPort(IOutputPort port)
    {
        if (port == null)
            throw new ArgumentNullException(nameof(port));
        _outputPorts.Add(port);
    }

    public abstract Task ProcessAsync();
} 