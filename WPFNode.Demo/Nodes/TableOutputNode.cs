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
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Demo.Nodes;

[NodeCategory("Table")]
[NodeName("Table Output")]
[NodeDescription("테이블 데이터를 출력하는 노드입니다.")]
public class TableOutputNode : DynamicNode, IDisposable {
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; private set; }
    
    [NodeFlowOut]
    public IFlowOutPort FlowOut { get; private set; }
    
    [NodeProperty("Target Type")]
    public NodeProperty<Type> SelectedType { get; private set; }

    [NodeProperty("Key Member", OnValueChanged = nameof(OnKeyMemberChanged))]
    [NodeDropDown(nameof(GetKeyMemberOptions))]
    public NodeProperty<string> SelectedKeyMember { get; private set; }

    private Type? _targetType;
    private object? _resultObject;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    private bool _disposed = false;
    
    private readonly List<INodeProperty> _columnProperties = new();

    [JsonConstructor]
    public TableOutputNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        Name = "Table Output";
        Description = "테이블 데이터를 특정 타입으로 변환하여 출력하는 노드입니다.";

        // Property 변경 이벤트 구독
        if (SelectedType != null)
        {
            SelectedType.PropertyChanged += SelectedType_PropertyChanged;
        }

        ReconfigureInputPorts();
    }

    private void OnKeyMemberChanged()
    {
        // 키 멤버가 변경되었을 때의 처리
    }

    private void SelectedType_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INodeProperty.Value) && SelectedType != null)
        {
            TargetType = SelectedType.Value as Type;
        }
    }

    // 키 멤버 옵션 제공 메서드
    private IEnumerable<string> GetKeyMemberOptions()
    {
        if (_targetType == null)
            return Array.Empty<string>();

        // 타입의 모든 프로퍼티 이름 반환
        return _targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                          .Select(p => p.Name)
                          .ToList();
    }

    public Type? TargetType {
        get => _targetType;
        set {
            _targetType = value;
            if (_targetType != null) {
                Name = $"Table Output ({_targetType.Name})";
                ReconfigureInputPorts();

                // 타입이 변경되면 키 멤버 목록도 함께 업데이트
                // 첫 번째 프로퍼티를 기본 키로 설정
                var properties = _targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                if (properties.Any()) {
                    // 첫 번째 멤버를 Key Member로 설정
                    SelectedKeyMember.Value = properties.First().Name;
                }
            }
        }
    }

    private void ReconfigureInputPorts() {
        if (_targetType == null) return;
        
        foreach(var property in _columnProperties)
        {
            // 기존 포트 제거
            Remove(property);
        }
        _columnProperties.Clear();

        // 타입의 public 프로퍼티들을 입력 포트로 생성
        var properties = _targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(p => p.CanWrite); // 쓰기 가능한 프로퍼티만 선택

        foreach (var prop in properties) {
            var nodeProperty = AddProperty(prop.Name, prop.Name, prop.PropertyType, canConnectToPort: true);
            _columnProperties.Add(nodeProperty);
        }
    }

    // 결과 객체를 반환합니다.
    public object? Result => _resultObject;

    // 결과 객체를 JSON 문자열로 직렬화하여 반환합니다.
    public string ResultJson
        => _resultObject != null
               ? JsonSerializer.Serialize(_resultObject, _targetType, _jsonOptions)
               : string.Empty;

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken) {
        if (_targetType == null) {
            throw new InvalidOperationException("대상 타입이 지정되지 않았습니다.");
        }

        try {
            // 새 인스턴스 생성
            _resultObject = Activator.CreateInstance(_targetType);
            if (_resultObject == null) {
                throw new InvalidOperationException("객체 생성에 실패했습니다.");
            }

            // 각 프로퍼티에 대해 입력 포트의 값을 설정
            var properties = _targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .Where(p => p.CanWrite);

            foreach (var prop in properties) {
                var port  = Properties.FirstOrDefault(p => p.Name == prop.Name);
                var value = port.Value;

                // 포트의 값을 프로퍼티에 설정
                prop.SetValue(_resultObject, Convert.ChangeType(value, prop.PropertyType));
                Console.WriteLine($"{prop.Name} - {value}");
            }
        }
        catch (Exception ex) {
            throw new InvalidOperationException($"객체 처리 중 오류 발생: {ex.Message}", ex);
        }

        yield return FlowOut;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;

        if (disposing) {
            if (SelectedType != null)
            {
                SelectedType.PropertyChanged -= SelectedType_PropertyChanged;
            }
        }

        _disposed = true;
    }
}
