using System.ComponentModel;
using System.Text.Json;
using WPFNode.Interfaces;
using WPFNode.Utilities;
using WPFNode.Exceptions;

namespace WPFNode.Models;

public class InputPort<T> : IInputPort<T>, INotifyPropertyChanged {
    protected readonly Dictionary<Type, Func<object, T>> _converters = new();
    protected readonly int                               _index;
    protected          bool                              _isVisible = true;
    protected          IConnection?                      _connection;

    public event PropertyChangedEventHandler? PropertyChanged;

    public InputPort(string name, INode node, int index) {
        Name   = name;
        Node   = node;
        _index = index;
    }

    public PortId Id          => new(Node.Guid, true, Name);
    public string Name        { get; set; }
    public Type   DataType    => typeof(T);
    public bool   IsInput     => true;
    public bool   IsConnected => _connection != null;

    public bool IsVisible {
        get => _isVisible;
        set {
            if (_isVisible != value) {
                if (!value && IsConnected) {
                    _connection?.Disconnect();
                }

                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public IReadOnlyList<IConnection> Connections    => _connection != null ? new[] { _connection } : Array.Empty<IConnection>();
    public INode                      Node           { get; private set; }
    public int                        GetPortIndex() => _index;

    public void RegisterConverter<TSource>(Func<TSource, T> converter) {
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));

        _converters[typeof(TSource)] = obj => converter((TSource)obj);
    }

    /// <summary>
    /// 주어진 타입이 컬렉션 타입인지 확인하고, 요소 타입을 반환합니다.
    /// </summary>
    protected bool IsCollectionType(Type type, out Type? elementType) {
        // TypeUtility의 GetElementType 메서드 활용
        elementType = WPFNode.Utilities.TypeExtensions.GetElementType(type);
        return elementType != null;
    }
    
    public virtual bool CanAcceptType(Type sourceType) {
        // 1. 직접 타입 체크
        if (DirectTypeCheck(sourceType))
            return true;
        
        // 2. 컬렉션 타입 호환성 체크
        if (CollectionTypeCheck(sourceType))
            return true;
        
        return false;
    }
    
    /// <summary>
    /// 기본 타입 호환성 검사를 수행합니다.
    /// </summary>
    protected virtual bool DirectTypeCheck(Type sourceType) {
        // 1. 컨버터가 등록되어 있으면 변환 가능
        if (_converters.ContainsKey(sourceType))
            return true;

        // 2. 동일한 타입인 경우
        if (sourceType == typeof(T))
            return true;

        // 3. 기본 타입 변환 가능 여부 확인
        if (sourceType.CanConvertTo(typeof(T)))
            return true;
            
        return false;
    }
    
    /// <summary>
    /// 컬렉션 타입 호환성 검사를 수행합니다.
    /// </summary>
    protected virtual bool CollectionTypeCheck(Type sourceType) {
        Type? sourceElementType = null;
        Type? targetElementType = null;
        
        // 양쪽 모두 컬렉션인지 확인
        bool sourceIsCollection = IsCollectionType(sourceType, out sourceElementType);
        bool targetIsCollection = IsCollectionType(typeof(T), out targetElementType);
        
        // 양쪽 모두 컬렉션이고 요소 타입이 추출되었으면 요소 타입의 호환성 확인
        if (sourceIsCollection && targetIsCollection && 
            sourceElementType != null && targetElementType != null) {
            return sourceElementType == targetElementType || 
                   sourceElementType.CanConvertTo(targetElementType);
        }
        
        return false;
    }

    public virtual T? GetValueOrDefault(T? defaultValue = default) {
        if (_connection?.Source is not { } outputPort || outputPort.Value == null)
            return defaultValue;
        
        var sourceValue = outputPort.Value;
        
        try {
            // 1. 등록된 컨버터 시도
            if (TryUseRegisteredConverter(sourceValue, out var convertedByCustom))
                return convertedByCustom;
            
            // 2. 직접 타입 변환 가능한 경우
            if (sourceValue is T typedValue)
                return typedValue;
            
            // 3. 컬렉션 변환 시도
            if (TryCollectionConversion(sourceValue, out var convertedCollection))
                return convertedCollection;
            
            // 4. 일반적인 타입 변환 시도
            if (sourceValue.TryConvertTo(out T? convertedValue))
                return convertedValue;
            
            // 5. 마지막으로 문자열 변환 시도 (대상 타입이 string이 아닌 경우에만)
            if (typeof(T) != typeof(string) && sourceValue.ToString() is string stringValue) {
                if (stringValue.TryConvertTo(out convertedValue)) {
                    return convertedValue;
                }
            }
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"InputPort 값 변환 중 오류 발생: {ex.Message}");
        }
        
        return defaultValue;
    }
    
    /// <summary>
    /// 등록된 커스텀 컨버터를 사용하여 값 변환을 시도합니다.
    /// </summary>
    protected bool TryUseRegisteredConverter(object sourceValue, out T? result) {
        result = default;
        
        if (_converters.TryGetValue(sourceValue.GetType(), out var converter)) {
            result = converter(sourceValue);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 컬렉션 타입 변환을 시도합니다. IEnumerable<>을 기반으로 통합된 변환 로직입니다.
    /// </summary>
    protected bool TryCollectionConversion(object sourceValue, out T? result) {
        result = default;
        
        // 소스와 타겟의 요소 타입 확인
        if (!IsCollectionType(typeof(T), out Type? targetElementType) || 
            !IsCollectionType(sourceValue.GetType(), out Type? sourceElementType) ||
            targetElementType == null || sourceElementType == null) {
            return false;
        }
        
        // 소스가 IEnumerable이 아니면 변환 불가
        if (!(sourceValue is System.Collections.IEnumerable sourceCollection)) {
            return false;
        }
        
        try {
            // 요소들을 변환
            var convertedItems = ConvertCollectionItems(sourceCollection, targetElementType, sourceElementType);
            
            // 대상 타입에 따라 적절한 컬렉션으로 변환
            if (typeof(T).IsArray) {
                // 배열로 변환
                var array = Array.CreateInstance(targetElementType, convertedItems.Count);
                for (int i = 0; i < convertedItems.Count; i++) {
                    array.SetValue(convertedItems[i], i);
                }
                result = (T)(object)array;
            }
            else if (typeof(T).IsGenericType) {
                var genericTypeDef = typeof(T).GetGenericTypeDefinition();
                
                if (genericTypeDef == typeof(List<>) || 
                    genericTypeDef == typeof(IList<>) ||
                    genericTypeDef == typeof(IEnumerable<>) ||
                    genericTypeDef == typeof(ICollection<>)) {
                    // List 또는 List 기반 인터페이스로 변환
                    var listType = typeof(List<>).MakeGenericType(targetElementType);
                    var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                    
                    foreach (var item in convertedItems) {
                        list.Add(item);
                    }
                    result = (T)(object)list;
                }
                else if (genericTypeDef == typeof(HashSet<>)) {
                    // HashSet으로 변환
                    var hashSetType = typeof(HashSet<>).MakeGenericType(targetElementType);
                    var hashSet = Activator.CreateInstance(hashSetType)!;
                    var addMethod = hashSetType.GetMethod("Add")!;
                    
                    foreach (var item in convertedItems) {
                        addMethod.Invoke(hashSet, new[] { item });
                    }
                    result = (T)hashSet;
                }
                else {
                    return false;
                }
            }
            else {
                return false;
            }
            
            return true;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"컬렉션 변환 중 오류: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 컬렉션의 요소를 변환하여 새 리스트를 반환합니다.
    /// </summary>
    protected List<object> ConvertCollectionItems(System.Collections.IEnumerable sourceCollection, Type targetElementType, Type sourceElementType) {
        var result = new List<object>();
        
        foreach (var item in sourceCollection) {
            if (item != null) {
                // 요소 타입이 같으면 그대로 사용
                if (targetElementType.IsAssignableFrom(item.GetType())) {
                    result.Add(item);
                }
                // 요소 타입이 다르면 변환 시도
                else {
                    var convertedItem = item.TryConvertTo(targetElementType);
                    if (convertedItem != null) {
                        result.Add(convertedItem);
                    }
                }
            }
        }
        
        return result;
    }
    

    public void AddConnection(IConnection connection) {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        // 기존 연결이 있으면 제거
        _connection?.Disconnect();

        _connection = connection;
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
    }

    public void RemoveConnection(IConnection connection) {
        if (connection == null)
            throw new NodeConnectionException("연결이 null입니다.");
        if (!connection.Target.Equals(this))
            throw new NodeConnectionException("연결의 타겟 포트가 일치하지 않습니다.", this, connection.Target);

        if (_connection == connection) {
            _connection = null;
            OnPropertyChanged(nameof(Connections));
            OnPropertyChanged(nameof(IsConnected));
        }
    }

    public IConnection Connect(IOutputPort source) {
        if (source == null)
            throw new NodeConnectionException("소스 포트가 null입니다.");

        if (!CanAcceptType(source.DataType))
            throw new NodeConnectionException("타입이 호환되지 않습니다.", source, this);

        if (source.Node == Node)
            throw new NodeConnectionException("같은 노드의 포트와는 연결할 수 없습니다.", source, this);

        // 기존 연결이 있으면 삭제
        _connection?.Disconnect();

        // Canvas를 통해 새로운 연결 생성
        var canvas = ((NodeBase)Node!).Canvas;
        return canvas.Connect(source, this);
    }

    public IConnection Connect(IPort otherPort) {
        if (otherPort == null)
            throw new NodeConnectionException("대상 포트가 null입니다.");
            
        if (otherPort is IOutputPort outputPort) {
            return Connect(outputPort);
        }
        else {
            throw new NodeConnectionException("입력 포트는 다른 입력 포트와 연결할 수 없습니다.");
        }
    }

    public void Disconnect() {
        _connection?.Disconnect();
    }

    protected virtual void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void WriteJson(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        writer.WriteString("Name", Name);
        writer.WriteString("Type", DataType.AssemblyQualifiedName);
        writer.WriteNumber("Index", GetPortIndex());
        writer.WriteBoolean("IsVisible", IsVisible);
        writer.WriteEndObject();
    }

    public void ReadJson(JsonElement element, JsonSerializerOptions options) {
        if (element.TryGetProperty("Name", out var nameElement))
            Name = nameElement.GetString()!;
        if (element.TryGetProperty("IsVisible", out var visibleElement))
            IsVisible = visibleElement.GetBoolean();
    }
}
