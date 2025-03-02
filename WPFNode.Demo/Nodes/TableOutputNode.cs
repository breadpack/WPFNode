using System.Reflection;
using System.Text.Json.Serialization;
using WPFNode.Attributes;
using WPFNode.Demo.Models;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Demo.Nodes;

[NodeCategory("Table")]
public class TableOutputNode : NodeBase
{
    private Type? _targetType;
    private object? _result;

    [JsonConstructor]
    public TableOutputNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Table Output";
        Description = "테이블 데이터를 특정 타입으로 변환하여 출력하는 노드입니다.";

        // 테이블 데이터 입력 포트 생성
        InputPort = CreateInputPort<TableData>("Table");
    }

    public InputPort<TableData> InputPort { get; set; }

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
            ProcessAsync().Wait();
        }
    }

    private void ReconfigureInputPorts()
    {
        if (_targetType == null) return;

        // 기존 포트 제거
        ClearPorts();

        // 테이블 데이터 입력 포트 다시 생성
        CreateInputPort("__Table", typeof(TableData));

        // 타입의 public 프로퍼티들을 입력 포트로 생성
        var properties = _targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite); // 쓰기 가능한 프로퍼티만 선택

        foreach (var prop in properties)
        {
            CreateInputPort(prop.Name, prop.PropertyType);
        }
    }

    public object? Result => _result;

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var tableData = InputPort.GetValueOrDefault();
        if (tableData != null && _targetType != null)
        {
            try
            {
                // 새 인스턴스 생성
                _result = Activator.CreateInstance(_targetType);

                // 각 프로퍼티에 대해 입력 포트의 값을 설정
                var properties = _targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite);

                foreach (var prop in properties) {
                    var port     = InputPorts.FirstOrDefault(p => p.Name == prop.Name);
                    var portType = typeof(InputPort<>).MakeGenericType(prop.PropertyType);
                    var value = portType.GetMethod(nameof(InputPort.GetValueOrDefault), BindingFlags.Public | BindingFlags.Instance)
                                        ?.Invoke(port, null);

                    // 포트의 값을 프로퍼티에 설정
                    prop.SetValue(_result, Convert.ChangeType(value, prop.PropertyType));
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"테이블 데이터를 {_targetType.Name} 타입으로 변환하는 중 오류 발생: {ex.Message}", ex);
            }
        }
        await Task.CompletedTask;
    }

    public override async Task SetParameterAsync(object parameter)
    {
        if (parameter is Type type)
        {
            TargetType = type;
        }
        await base.SetParameterAsync(parameter);
    }
} 