using System.ComponentModel;
using System.Text.Json;
using WPFNode.Interfaces;
using WPFNode.Utilities;
using WPFNode.Exceptions;

namespace WPFNode.Models;

public class InputPort<T> : IInputPort, INotifyPropertyChanged {
    private readonly List<IConnection>                 _connections = new();
    private readonly Dictionary<Type, Func<object, T>> _converters  = new();
    private readonly int                               _index;
    private bool                                      _isVisible = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public InputPort(string name, INode node, int index) {
        Name   = name;
        Node   = node;
        _index = index;
    }

    public PortId                     Id             => new(Node.Id, true, _index);
    public string                     Name           { get; set; }
    public Type                       DataType       => typeof(T);
    public bool                       IsInput        => true;
    public bool                       IsConnected    => _connections.Count > 0;
    public bool                       IsVisible      
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                if (!value && IsConnected)
                {
                    // IsVisible이 false로 설정되고 연결이 있는 경우에만 연결 해제
                    foreach (var connection in Connections.ToArray())
                    {
                        connection.Disconnect();
                    }
                }
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }
    public IReadOnlyList<IConnection> Connections    => _connections;
    public INode                      Node           { get; private set; }
    public int                        GetPortIndex() => _index;

    public void RegisterConverter<TSource>(Func<TSource, T> converter) {
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));

        _converters[typeof(TSource)] = obj => converter((TSource)obj);
    }

    public bool CanAcceptType(Type sourceType) {
        // 1. 컨버터가 등록되어 있으면 변환 가능
        if (_converters.ContainsKey(sourceType))
            return true;

        // 2. 대상 타입이 string이면 모든 타입 허용 (ToString 메서드를 통해 변환 가능)
        if (typeof(T) == typeof(string))
            return true;

        // 3. 타입 호환성 검사
        return sourceType.CanImplicitlyConvertTo(typeof(T));
    }

    public object? Value {
        get {
            // 연결된 OutputPort로부터 값을 가져옴
            if (_connections.Count > 0 && _connections[0].Source is IOutputPort outputPort) {
                var sourceValue = outputPort.Value;
                if (sourceValue == null) return default(T);

                // 1. 컨버터를 통한 변환 시도
                if (_converters.TryGetValue(sourceValue.GetType(), out var converter)) {
                    return converter(sourceValue);
                }

                // 2. 타입 변환 시도
                if (sourceValue.TryConvertTo(out T? convertedValue)) {
                    return convertedValue;
                }
            }

            return default(T);
        }
    }

    public T? GetValueOrDefault(T? defaultValue = default) {
        var value = Value;
        return value is T typedValue ? typedValue : defaultValue;
    }

    public void AddConnection(IConnection connection) {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        _connections.Add(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
    }

    public void RemoveConnection(IConnection connection)
    {
        if (connection == null)
            throw new NodeConnectionException("연결이 null입니다.");
        if (!connection.Target.Equals(this))
            throw new NodeConnectionException("연결의 타겟 포트가 일치하지 않습니다.", this, connection.Target);
            
        _connections.Remove(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
    }

    public IConnection Connect(IOutputPort source)
    {
        if (source == null)
            throw new NodeConnectionException("소스 포트가 null입니다.");
            
        if (!CanAcceptType(source.DataType))
            throw new NodeConnectionException("타입이 호환되지 않습니다.", source, this);
            
        if (source.Node == Node)
            throw new NodeConnectionException("같은 노드의 포트와는 연결할 수 없습니다.", source, this);
            
        // Canvas를 통해 연결 생성
        var canvas = ((NodeBase)Node!).Canvas;
        return canvas.Connect(source, this);
    }

    protected virtual void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("Name", Name);
        writer.WriteString("Type", DataType.AssemblyQualifiedName);
        writer.WriteNumber("Index", GetPortIndex());
        writer.WriteBoolean("IsVisible", IsVisible);
        writer.WriteEndObject();
    }

    public void ReadJson(JsonElement element)
    {
        if (element.TryGetProperty("Name", out var nameElement))
            Name = nameElement.GetString()!;
        if (element.TryGetProperty("IsVisible", out var visibleElement))
            IsVisible = visibleElement.GetBoolean();
    }
}