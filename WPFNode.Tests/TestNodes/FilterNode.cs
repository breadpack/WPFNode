using System.Text.Json.Serialization;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Tests.TestNodes;

/// <summary>
/// 입력된 숫자가 특정 조건을 만족하는지 확인하는 노드
/// </summary>
public class FilterNode : NodeBase, IResettable {
    private Func<int, bool> _filterCondition;
    private bool _debugMode = true;
    private bool _hasProcessed = false; // 값이 처리되었는지 추적

    [JsonConstructor]
    public FilterNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        Name = "Filter";
        InputPort = CreateInputPort<int>("Input");
        ConditionPort = CreateInputPort<bool>("UseCondition");
        IsValidPort = CreateOutputPort<bool>("IsValid");
        ValuePort = CreateOutputPort<int>("Value");
        HasProcessedPort = CreateOutputPort<bool>("HasProcessed");

        // 기본 필터 조건은 짝수 확인
        _filterCondition = value => value % 2 == 0;
    }

    public OutputPort<int> ValuePort { get; set; }
    public OutputPort<bool> IsValidPort { get; set; }
    public OutputPort<bool> HasProcessedPort { get; set; }
    public InputPort<int> InputPort { get; set; }
    public InputPort<bool> ConditionPort { get; set; }

    // 필터링 조건 속성
    public Func<int, bool> FilterCondition {
        get => _filterCondition;
        set => _filterCondition = value ?? (x => x % 2 == 0);
    }

    public void Reset() {
        _hasProcessed = false;
        if (_debugMode) {
            Console.WriteLine("FilterNode: Reset called");
        }
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default) {
        var value = InputPort.GetValueOrDefault();
        var useCondition = ConditionPort.GetValueOrDefault(true);

        bool isValid = _filterCondition(value);
        _hasProcessed = true;
        
        if (_debugMode) {
            Console.WriteLine($"FilterNode: input={value}, condition={isValid}, useCondition={useCondition}");
        }

        IsValidPort.Value = isValid;
        HasProcessedPort.Value = _hasProcessed;
        
        // 중요: 조건에 맞는 경우에만 ValuePort에 값을 설정
        if (isValid) {
            ValuePort.Value = value;
            if (_debugMode) Console.WriteLine($"FilterNode: ValuePort 출력 = {value}");
        }

        await Task.CompletedTask;
    }
} 