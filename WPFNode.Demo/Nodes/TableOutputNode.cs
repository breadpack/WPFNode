using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Demo.Models;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;

namespace WPFNode.Demo.Nodes;

[NodeCategory("Table")]
[NodeName("Table Output")]
[NodeDescription("테이블 데이터를 출력하는 노드입니다.")]
public class TableOutputNode : DynamicNode, IDisposable
{
    private INodeProperty? _selectedType;
    private Type? _targetType;
    private object? _resultObject;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };
    private bool _isInitialized = false;
    private bool _isPropertyChangeHandlerAttached = false;
    private bool _disposed = false;

    [JsonConstructor]
    public TableOutputNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Table Output";
        Description = "테이블 데이터를 특정 타입으로 변환하여 출력하는 노드입니다.";
        
        Initialize();
        
        // 속성 변경 이벤트 구독
        PropertyChanged += TableOutputNode_PropertyChanged;
    }
    
    private void TableOutputNode_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Properties 컬렉션이 변경되었을 때 이벤트 핸들러 재연결
        if (e.PropertyName == nameof(Properties))
        {
            AttachPropertyChangeHandlers();
        }
    }
    
    private void AttachPropertyChangeHandlers()
    {
        // 이전 이벤트 핸들러가 있다면 제거
        if (_isPropertyChangeHandlerAttached && _selectedType is INotifyPropertyChanged oldNotifyPropertyChanged)
        {
            oldNotifyPropertyChanged.PropertyChanged -= SelectedType_PropertyChanged;
            _isPropertyChangeHandlerAttached = false;
        }
            
        // _selectedType 찾기
        if (Properties.TryGetValue("selectedType", out var selectedTypeProperty))
        {
            _selectedType = selectedTypeProperty;
            
            // 타입 변경 감지
            if (_selectedType is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += SelectedType_PropertyChanged;
                _isPropertyChangeHandlerAttached = true;
            }
        }
    }
    
    private void SelectedType_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INodeProperty.Value) && _selectedType != null)
        {
            TargetType = _selectedType.Value as Type;
        }
    }
    
    private void Initialize()
    {
        if (_isInitialized) return;
        
        // Type 선택 프로퍼티 생성
        _selectedType = AddProperty<Type>("selectedType", "Target Type");
        
        // 먼저 초기값 설정
        _selectedType.Value = typeof(object);
        _targetType = typeof(object);
        Name = $"Table Output ({_targetType.Name})";
        
        // 포트 구성 (이벤트 핸들러 연결 전에 실행)
        ReconfigureInputPorts();
        
        // 이벤트 핸들러는 초기 구성 후 연결
        AttachPropertyChangeHandlers();
        
        _isInitialized = true;
    }

    public Type? TargetType
    {
        get => _targetType;
        set
        {
            _targetType = value;
            if (_targetType != null)
            {
                Name = $"Table Output ({_targetType.Name})";
                ReconfigureInputPorts();
            }
        }
    }

    private void ReconfigureInputPorts()
    {
        if (_targetType == null) return;

        // 기존 포트 제거 (selectedType 속성은 유지)
        ClearPorts("selectedType");

        // 타입의 public 프로퍼티들을 입력 포트로 생성
        var properties = _targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite); // 쓰기 가능한 프로퍼티만 선택

        foreach (var prop in properties)
        {
            AddProperty(prop.Name, prop.Name, prop.PropertyType, canConnectToPort: true);
        }
    }

    // 결과 객체를 반환합니다.
    public object? Result => _resultObject;

    // 결과 객체를 JSON 문자열로 직렬화하여 반환합니다.
    public string ResultJson => _resultObject != null 
        ? JsonSerializer.Serialize(_resultObject, _targetType, _jsonOptions) 
        : string.Empty;

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        if (_targetType == null) return;

        try
        {
            // 새 인스턴스 생성
            _resultObject = Activator.CreateInstance(_targetType);
            if (_resultObject == null) return;

            // 각 프로퍼티에 대해 입력 포트의 값을 설정
            var properties = _targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);

            foreach (var prop in properties)
            {
                var port = InputPorts.FirstOrDefault(p => p.Name == prop.Name);
                if (port == null) continue;

                var portType = typeof(InputPort<>).MakeGenericType(prop.PropertyType);
                var getValue = portType.GetMethod(nameof(InputPort<object>.GetValueOrDefault), 
                    BindingFlags.Public | BindingFlags.Instance);
                
                if (getValue == null) continue;
                
                var value = getValue.Invoke(port, null);
                
                // 포트의 값을 프로퍼티에 설정
                if (value != null)
                {
                    prop.SetValue(_resultObject, Convert.ChangeType(value, prop.PropertyType));
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"객체 처리 중 오류 발생: {ex.Message}", ex);
        }

        await Task.CompletedTask;
    }

    public override async Task SetParameterAsync(object parameter)
    {
        if (parameter is Type type)
        {
            _selectedType.Value = type;
            TargetType = type;
        }
        
        await base.SetParameterAsync(parameter);
    }
    
    // ReadJson 메서드를 오버라이드하여 JSON 역직렬화 시 Initialize()가 중복 실행되지 않도록 함
    public override void ReadJson(JsonElement element, JsonSerializerOptions options)
    {
        // 기본 역직렬화 수행
        base.ReadJson(element, options);
        
        // JSON 역직렬화 후에는 이미 초기화된 상태로 간주
        // 이렇게 하면 생성자에서 호출하는 Initialize()가 실행되지 않음
        _isInitialized = true;
        
        // _selectedType 찾아서 이벤트 핸들러 연결
        AttachPropertyChangeHandlers();
        
        ReconfigureInputPorts();
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
            
        if (disposing)
        {
            // 이벤트 구독 해제
            PropertyChanged -= TableOutputNode_PropertyChanged;
            
            if (_selectedType is INotifyPropertyChanged notifyPropertyChanged && _isPropertyChangeHandlerAttached)
            {
                notifyPropertyChanged.PropertyChanged -= SelectedType_PropertyChanged;
                _isPropertyChangeHandlerAttached = false;
            }
        }
        
        _disposed = true;
    }
}
