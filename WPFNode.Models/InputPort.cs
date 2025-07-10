using System; // Added for Type
using System.Collections.Generic; // Added for Dictionary, IReadOnlyList
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization; // Added for JsonIgnore
using WPFNode.Interfaces;
using WPFNode.Utilities;
using WPFNode.Exceptions;

namespace WPFNode.Models;

public class InputPort<T> : IInputPort<T>, INotifyPropertyChanged {
    protected readonly Dictionary<Type, Func<object, T>> _converters = new();
    protected readonly int                               _index;
    protected          bool                              _isVisible = true;
    protected          IConnection?                      _connection;
    private            Type?                             _connectedType; // Cache for ConnectedType

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

    /// <summary>
    /// 연결된 OutputPort의 데이터 타입입니다. 연결되지 않은 경우 null입니다.
    /// </summary>
    [JsonIgnore] // 직렬화에서 제외
    public Type? ConnectedType => _connectedType;

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
        if (sourceIsCollection && targetIsCollection) {
            // 타겟 요소 타입이 있는 경우
            if (targetElementType != null) {
                // 소스 요소 타입이 있는 경우 - 일반적인 호환성 확인
                if (sourceElementType != null) {
                    return sourceElementType == targetElementType || 
                           sourceElementType.CanConvertTo(targetElementType);
                }
                // 소스가 비제네릭 컬렉션인 경우 (예: IList, ICollection)
                else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(sourceType)) {
                    // 비제네릭 IList -> List<int>와 같은 변환을 허용
                    if (typeof(System.Collections.IList).IsAssignableFrom(sourceType) ||
                        typeof(System.Collections.ICollection).IsAssignableFrom(sourceType)) {
                        return true; // 런타임에 변환 시도
                    }
                }
            }
        }
        
        // 🆕 단일 요소 → 컨테이너 변환 지원
        if (!sourceIsCollection && targetIsCollection && targetElementType != null) {
            // 소스 타입이 타겟 요소 타입과 호환되는지 확인
            return sourceType == targetElementType || sourceType.CanConvertTo(targetElementType);
        }
        
        return false;
    }

    /// <summary>
    /// 변환 없이 연결된 OutputPort에서 직접 값을 가져옵니다.
    /// 컬렉션의 참조 일관성을 유지하기 위해 사용됩니다.
    /// </summary>
    protected object? GetValueWithoutConversion() {
        if (_connection?.Source is not IOutputPort { } outputPort)
            return null;
            
        return outputPort.Value;
    }
    
    // 간소화된 디버그 로깅을 위한 유틸리티 메서드
    private void LogDebug(string message, object? value = null) {
        if (value != null)
            System.Diagnostics.Debug.WriteLine($"{message}, HashCode: {value.GetHashCode()}");
        else
            System.Diagnostics.Debug.WriteLine(message);
    }

    public object? Value => GetValueOrDefault();
    
    public virtual T? GetValueOrDefault(T? defaultValue = default) {
        if (_connection?.Source is not IOutputPort { } outputPort || outputPort.Value == null)
            return defaultValue;
        
        var sourceValue = outputPort.Value;
        
        try {
            // 1. 등록된 컨버터 시도
            if (TryUseRegisteredConverter(sourceValue, out var convertedByCustom)) {
                return convertedByCustom;
            }
            
            // 2. 직접 타입 변환 가능한 경우
            if (sourceValue is T typedValue) {
                LogDebug("직접 타입 변환", sourceValue);
                return typedValue;
            }
            
            // 3. 컬렉션 변환 시도
            if (TryCollectionConversion(sourceValue, out var convertedCollection)) {
                LogDebug("컬렉션 변환 성공", convertedCollection);
                return convertedCollection;
            }
            
            // 4. 일반적인 타입 변환 시도
            if (sourceValue.TryConvertTo(out T? convertedValue)) {
                return convertedValue;
            }
            
            // 5. 마지막으로 문자열 변환 시도 (대상 타입이 string이 아닌 경우에만)
            if (typeof(T) != typeof(string) && sourceValue.ToString() is string stringValue) {
                if (stringValue.TryConvertTo(out convertedValue)) {
                    return convertedValue;
                }
            }
        }
        catch (Exception ex) {
            LogDebug($"InputPort 값 변환 중 오류 발생: {ex.Message}");
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
    /// 두 컬렉션 타입 간의 호환성을 검사합니다.
    /// </summary>
    protected bool AreCollectionTypesCompatible(Type sourceType, Type targetType) {
        // 직접 할당 가능한 경우 (가장 빠른 경로)
        if (targetType.IsAssignableFrom(sourceType)) {
            return true;
        }
        
        // 둘 다 컬렉션인지 확인
        bool sourceIsCollection = IsCollectionType(sourceType, out Type? sourceElementType);
        bool targetIsCollection = IsCollectionType(targetType, out Type? targetElementType);
        
        if (!sourceIsCollection || !targetIsCollection || 
            sourceElementType == null || targetElementType == null) {
            return false;
        }
        
        // 요소 타입 호환성 확인
        bool elementsCompatible = targetElementType.IsAssignableFrom(sourceElementType) || 
                                sourceElementType.CanConvertTo(targetElementType);
        
        if (!elementsCompatible) {
            return false;
        }
        
        // 컬렉션 인터페이스 호환성 검사
        // 1. 둘 다 제네릭 컬렉션 인터페이스 기반인지
        bool bothGenericCollections = 
            (sourceType.IsGenericType && targetType.IsGenericType) && 
            (typeof(IEnumerable<>).IsAssignableFrom(sourceType.GetGenericTypeDefinition()) ||
             typeof(ICollection<>).IsAssignableFrom(sourceType.GetGenericTypeDefinition()) ||
             typeof(IList<>).IsAssignableFrom(sourceType.GetGenericTypeDefinition())) &&
            (typeof(IEnumerable<>).IsAssignableFrom(targetType.GetGenericTypeDefinition()) ||
             typeof(ICollection<>).IsAssignableFrom(targetType.GetGenericTypeDefinition()) ||
             typeof(IList<>).IsAssignableFrom(targetType.GetGenericTypeDefinition()));
             
        // 2. 소스가 더 구체적인 구현인지 (IEnumerable<T> <- ICollection<T> <- IList<T> <- List<T>)
        bool sourceMoreSpecific = IsMoreSpecificCollection(sourceType, targetType);
        
        return bothGenericCollections || sourceMoreSpecific;
    }

    /// <summary>
    /// 소스 컬렉션 타입이 타겟 컬렉션 타입보다 더 구체적인 구현인지 확인
    /// (예: List<T>는 IList<T>보다 더 구체적)
    /// </summary>
    protected bool IsMoreSpecificCollection(Type sourceType, Type targetType) {
        // 타겟이 인터페이스인 경우
        if (targetType.IsInterface) {
            // 소스가 해당 인터페이스를 구현하는지 확인 (제네릭 파라미터 고려)
            if (sourceType.GetInterfaces().Any(i => 
                i.IsGenericType && 
                i.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition() && 
                i.GetGenericArguments().Length == targetType.GetGenericArguments().Length &&
                Enumerable.Range(0, i.GetGenericArguments().Length).All(idx => 
                    i.GetGenericArguments()[idx].IsAssignableFrom(targetType.GetGenericArguments()[idx]) ||
                    targetType.GetGenericArguments()[idx].IsAssignableFrom(i.GetGenericArguments()[idx])))) {
                return true;
            }
        }
        
        // 구현 클래스 계층 구조 확인
        // 예: LinkedList<T> -> ICollection<T>, SortedSet<T> -> ISet<T> 등
        var sourceCollectionInterfaces = sourceType.GetInterfaces()
            .Where(i => i.IsGenericType && typeof(IEnumerable<>).IsAssignableFrom(i.GetGenericTypeDefinition()))
            .ToList();
            
        if (targetType.IsGenericType) {
            return sourceCollectionInterfaces.Any(i => 
                i.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition() &&
                Enumerable.Range(0, i.GetGenericArguments().Length).All(idx => 
                    i.GetGenericArguments()[idx].IsAssignableFrom(targetType.GetGenericArguments()[idx]) ||
                    targetType.GetGenericArguments()[idx].IsAssignableFrom(i.GetGenericArguments()[idx])));
        }
        
        return false;
    }

    /// <summary>
    /// 컬렉션 타입 변환을 시도합니다. 원본 참조를 최대한 유지하는 데 중점을 둡니다.
    /// </summary>
    protected bool TryCollectionConversion(object sourceValue, out T? result) {
        result = default;
        
        // 1. 소스가 이미 T 타입이면 변환 없이 바로 반환 (가장 빠른 경로)
        if (sourceValue is T directMatch) {
            result = directMatch;
            LogDebug("직접 타입 일치", sourceValue);
            return true;
        }
        
        Type sourceType = sourceValue.GetType();
        Type targetType = typeof(T);
        
        // 2. 타입 호환성 검사를 통한 직접 캐스팅 시도
        if (TryDirectCasting(sourceValue, out result)) {
            LogDebug("직접 캐스팅 성공 - 참조 유지", result);
            return true;
        }
        
        // 3. 변환이 필요한 경우 - 참조 유지를 최대화하며 변환
        if (TryCreateCollection(sourceValue, out result)) {
            LogDebug("컬렉션 변환 완료 - 새 컬렉션", result);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 직접 캐스팅을 시도합니다. (참조 보존)
    /// </summary>
    private bool TryDirectCasting(object sourceValue, out T? result) {
        result = default;
        Type sourceType = sourceValue.GetType();
        Type targetType = typeof(T);
        
        try {
            // 1. 인터페이스 호환성
            if (targetType.IsInterface && targetType.IsAssignableFrom(sourceType)) {
                result = (T)sourceValue; // 원본 그대로 반환
                LogDebug("인터페이스 호환성 - 원본 그대로 반환", sourceValue);
                return true;
            }
            
            // 2. 정확한 타입 일치
            if (sourceType == targetType) {
                result = (T)sourceValue;
                LogDebug("정확한 타입 일치 - 원본 그대로 반환", sourceValue);
                return true;
            }
            
            // 3. 컬렉션 타입 호환성
            if (AreCollectionTypesCompatible(sourceType, targetType)) {
                try {
                    result = (T)sourceValue; // 직접 캐스팅
                    LogDebug("컬렉션 호환성 - 직접 캐스팅", sourceValue);
                    return true;
                }
                catch {
                    // 캐스팅 실패 시, 원본 값이 있으면 사용
                    if (IsConnected) {
                        var storedValue = GetValueWithoutConversion();
                        if (storedValue != null && storedValue is T storedT) {
                            result = storedT;
                            LogDebug("기존 연결의 값 직접 사용", storedValue);
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        catch (Exception ex) {
            LogDebug($"직접 캐스팅 실패: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 새 컬렉션을 생성하되, 요소 참조는 최대한 유지합니다.
    /// </summary>
    private bool TryCreateCollection(object sourceValue, out T? result) {
        result = default;
        Type targetType = typeof(T);
        
        // 타겟이 컬렉션 타입인지 확인
        if (!IsCollectionType(targetType, out Type? targetElementType) || targetElementType == null) {
            return false;
        }
        
        List<object> items;
        
        // 🆕 소스가 IEnumerable인지 확인하고, 아니면 단일 요소로 처리
        // 단, string은 IEnumerable<char>이지만 단일 요소로 처리
        if (sourceValue is System.Collections.IEnumerable sourceCollection && sourceValue.GetType() != typeof(string)) {
            // 기존 로직: 컬렉션 → 컬렉션
            bool sourceIsGenericCollection = IsCollectionType(sourceValue.GetType(), out Type? sourceElementType);
            items = CollectItems(sourceCollection, targetElementType, sourceIsGenericCollection, sourceElementType);
        } else {
            // 🆕 새로운 로직: 단일 요소 → 컨테이너 (string 포함)
            items = CollectSingleItem(sourceValue, targetElementType);
        }
        
        try {
            if (items.Count == 0) {
                return false;
            }
            
            // 대상 타입에 따라 적절한 컬렉션 생성
            if (targetType.IsArray) {
                return CreateArray(items, targetElementType, out result);
            }
            else if (targetType.IsGenericType) {
                var genericTypeDef = targetType.GetGenericTypeDefinition();
                
                if (genericTypeDef == typeof(List<>) || 
                    genericTypeDef == typeof(IList<>) ||
                    genericTypeDef == typeof(IEnumerable<>) ||
                    genericTypeDef == typeof(ICollection<>)) {
                    return CreateList(items, targetElementType, out result);
                }
                else if (genericTypeDef == typeof(HashSet<>)) {
                    return CreateHashSet(items, targetElementType, out result);
                }
            }
            
            return false;
        }
        catch (Exception ex) {
            LogDebug($"컬렉션 생성 중 오류: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 소스 컬렉션에서 요소를 수집합니다. 타입 호환성에 따라 참조 유지 또는 변환합니다.
    /// </summary>
    private List<object> CollectItems(System.Collections.IEnumerable sourceCollection, Type targetElementType, 
                                     bool sourceIsGenericCollection, Type? sourceElementType) {
        var items = new List<object>();
        
        // 요소 타입 호환성 확인
        bool canPreserveReferences = sourceIsGenericCollection && sourceElementType != null && 
            (sourceElementType == targetElementType || targetElementType.IsAssignableFrom(sourceElementType));
        
        if (canPreserveReferences) {
            LogDebug($"요소 타입 호환 - 참조 보존 가능: {sourceElementType?.Name} -> {targetElementType.Name}");
            foreach (var item in sourceCollection) {
                if (item != null) {
                    items.Add(item); // 참조만 복사
                }
            }
        }
        else {
            LogDebug($"요소 타입 변환 필요: {sourceElementType?.Name} -> {targetElementType.Name}");
            foreach (var item in sourceCollection) {
                if (item != null) {
                    // 호환 가능한 타입이면 그대로 사용
                    if (targetElementType.IsAssignableFrom(item.GetType())) {
                        items.Add(item);
                    }
                    // 변환 필요
                    else {
                        var convertedItem = item.TryConvertTo(targetElementType);
                        if (convertedItem != null) {
                            items.Add(convertedItem);
                        }
                    }
                }
            }
        }
        
        return items;
    }
    
    /// <summary>
    /// 배열 생성
    /// </summary>
    private bool CreateArray(List<object> items, Type elementType, out T? result) {
        var array = Array.CreateInstance(elementType, items.Count);
        for (int i = 0; i < items.Count; i++) {
            array.SetValue(items[i], i);
        }
        result = (T)(object)array;
        LogDebug($"배열로 변환 완료: {items.Count}개 항목", array);
        return true;
    }
    
    /// <summary>
    /// List 생성
    /// </summary>
    private bool CreateList(List<object> items, Type elementType, out T? result) {
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
        
        foreach (var item in items) {
            list.Add(item);
        }
        result = (T)(object)list;
        LogDebug($"List<{elementType.Name}> 생성 완료: {items.Count}개 항목", list);
        return true;
    }
    
    /// <summary>
    /// HashSet 생성
    /// </summary>
    private bool CreateHashSet(List<object> items, Type elementType, out T? result) {
        var hashSetType = typeof(HashSet<>).MakeGenericType(elementType);
        var hashSet = Activator.CreateInstance(hashSetType)!;
        var addMethod = hashSetType.GetMethod("Add")!;
        
        foreach (var item in items) {
            addMethod.Invoke(hashSet, new[] { item });
        }
        result = (T)hashSet;
        LogDebug($"HashSet<{elementType.Name}> 생성 완료: {items.Count}개 항목", hashSet);
        return true;
    }
    
    /// <summary>
    /// 단일 요소를 컬렉션 항목으로 수집합니다. 기존 CollectItems 로직을 재사용합니다.
    /// </summary>
    private List<object> CollectSingleItem(object sourceValue, Type targetElementType) {
        var items = new List<object>();
        
        // 호환 가능한 타입이면 그대로 사용
        if (targetElementType.IsAssignableFrom(sourceValue.GetType())) {
            items.Add(sourceValue);
            LogDebug($"단일 요소 직접 추가: {sourceValue.GetType().Name} -> {targetElementType.Name}", sourceValue);
        }
        // 변환 필요
        else {
            var convertedItem = sourceValue.TryConvertTo(targetElementType);
            if (convertedItem != null) {
                items.Add(convertedItem);
                LogDebug($"단일 요소 변환 추가: {sourceValue.GetType().Name} -> {targetElementType.Name}", convertedItem);
            }
            else {
                LogDebug($"단일 요소 변환 실패: {sourceValue.GetType().Name} -> {targetElementType.Name}");
            }
        }
        
        return items;
    }
    
    public void AddConnection(IConnection connection) {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        // 기존 연결이 있으면 제거
        _connection?.Disconnect();

        _connection = connection;
        _connectedType = connection?.Source?.DataType; // 연결 시 ConnectedType 업데이트
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(ConnectedType)); // ConnectedType 변경 알림
    }

    public void RemoveConnection(IConnection connection) {
        if (connection == null)
            throw new NodeConnectionException("연결이 null입니다.");
        if (!connection.Target.Equals(this))
            throw new NodeConnectionException("연결의 타겟 포트가 일치하지 않습니다.", this, connection.Target);

        if (_connection == connection) {
            _connection = null;
            var previousConnectedType = _connectedType;
            _connectedType = null; // 연결 해제 시 ConnectedType 업데이트
            OnPropertyChanged(nameof(Connections));
            OnPropertyChanged(nameof(IsConnected));
            // 타입이 실제로 변경되었을 때만 알림 (null -> null 방지)
            if (previousConnectedType != null) {
                OnPropertyChanged(nameof(ConnectedType)); // ConnectedType 변경 알림
            }
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

    /// <summary>
    /// 포트 초기화 로직. InputPort<T>는 기본적으로 할 일이 없습니다.
    /// </summary>
    public virtual void Initialize() {
        // 기본 구현은 비어 있음
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
