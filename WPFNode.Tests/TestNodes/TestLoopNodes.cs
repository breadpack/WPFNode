using System.Text.Json.Serialization;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Tests.TestNodes;

/// <summary>
/// 숫자 시퀀스를 생성하는 Loop 노드
/// </summary>
public class NumberSequenceNode : NodeBase, ILoopNode, IResettable {
    private const int DefaultStart = 1;
    private const int DefaultEnd   = 5;

    private int  _current = DefaultStart;
    private int  _start   = DefaultStart;
    private int  _end     = DefaultEnd;
    private bool _isCompleted;

    [JsonConstructor]
    public NumberSequenceNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        Name = "Number Sequence";
        
        StartPort = CreateInputPort<int>("Start");
        EndPort   = CreateInputPort<int>("End");
        ResetPort = CreateInputPort<bool>("Reset");
        ResultPort = CreateOutputPort<int>("Current");
        IsLastPort = CreateOutputPort<bool>("IsLast");
    }

    public OutputPort<bool> IsLastPort { get; set; }

    public OutputPort<int> ResultPort { get; set; }

    public InputPort<bool> ResetPort { get; set; }

    public InputPort<int> EndPort { get; set; }

    public InputPort<int> StartPort { get; set; }

    public bool IsLoopCompleted => _isCompleted;

    public void Reset() {
        _start = StartPort.GetValueOrDefault(DefaultStart);
        _end   = Math.Max(_start, EndPort.GetValueOrDefault(DefaultEnd));
        _current = _start;
        _isCompleted = false;
    }

    public Task<bool> ShouldContinueAsync(CancellationToken cancellationToken = default) {
        return Task.FromResult(!_isCompleted);
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default) {
        if (ResetPort.GetValueOrDefault()) {
            Reset();
            return;
        }

        // 현재 값 출력
        ResultPort.Value = _current;
        
        // 현재 값이 마지막 값인지 확인
        var isLast = _current >= _end;
        IsLastPort.Value = isLast;
        
        // 다음 값으로 이동하거나 완료 상태로 전환
        if (isLast) {
            _isCompleted = true;
        } else {
            _current++;
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// 입력된 숫자를 누적하여 합계를 계산하는 노드
/// </summary>
public class AccumulatorNode : NodeBase, IResettable {
    private int _sum;
    private bool _debugMode = true;

    [JsonConstructor]
    public AccumulatorNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        Name = "Accumulator";
        
        InputPort = CreateInputPort<int>("Input");
        IsLastPort = CreateInputPort<bool>("IsLast");
        ResultPort = CreateOutputPort<int>("Sum");
        IsCompletePort = CreateOutputPort<bool>("IsComplete");
    }

    public OutputPort<bool> IsCompletePort { get; set; }

    public OutputPort<int> ResultPort { get; set; }

    public InputPort<bool> IsLastPort { get; set; }

    public InputPort<int> InputPort { get; set; }

    public int Sum => _sum;

    public void Reset() {
        _sum = 0;
        if (_debugMode) {
            Console.WriteLine($"AccumulatorNode: Reset - sum={_sum}");
        }
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default) {
        var input = InputPort.GetValueOrDefault();
        var isLast = IsLastPort.GetValueOrDefault();
        
        if (_debugMode) {
            Console.WriteLine($"AccumulatorNode: Processing - input={input}, isLast={isLast}, currentSum={_sum}");
        }
        
        // 입력값이 0이 아닐 때만 합산 (입력값이 0인 경우는 필터링된 경우일 수 있음)
        if (input != 0) {
            _sum += input;
            if (_debugMode) {
                Console.WriteLine($"AccumulatorNode: 값 더함 - input={input}, newSum={_sum}");
            }
        } else {
            if (_debugMode) {
                Console.WriteLine($"AccumulatorNode: 0값 무시함");
            }
        }
        
        ResultPort.Value = _sum;
        IsCompletePort.Value = isLast;
        
        if (_debugMode) {
            Console.WriteLine($"AccumulatorNode: After processing - newSum={_sum}, isComplete={isLast}");
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// 입력된 숫자의 배수를 생성하는 노드
/// </summary>
public class MultiplyNode : NodeBase {
    [JsonConstructor]
    public MultiplyNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        Name = "Multiply";
        InputPort = CreateInputPort<int>("Input");
        ResultPort = CreateOutputPort<int>("Result");
    }

    public OutputPort<int> ResultPort { get; set; }

    public InputPort<int> InputPort { get; set; }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default) {
        var value = InputPort.GetValueOrDefault();
        ResultPort.Value = value * 2; // 입력값을 2배로
        await Task.CompletedTask;
    }
}

/// <summary>
/// 결과를 수집하는 출력 노드
/// </summary>
[WPFNode.Attributes.OutputNode]
public class ResultCollectorNode : NodeBase, IResettable {
    private readonly List<int> _values = new();

    [JsonConstructor]
    public ResultCollectorNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        Name           = "Result Collector";
        ValuePort      = CreateInputPort<int>("Value");
        IsCompletePort = CreateInputPort<bool>("IsComplete");
    }

    public InputPort<bool> IsCompletePort { get; set; }

    public InputPort<int> ValuePort { get; set; }

    public IReadOnlyList<int> CollectedValues => _values;

    public void Reset() {
        _values.Clear();
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default) {
        // 값이 있으면 수집
        _values.Add(ValuePort.GetValueOrDefault());
        await Task.CompletedTask;
    }
}