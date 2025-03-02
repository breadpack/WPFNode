using System.Text.Json.Serialization;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Tests.TestNodes;

/// <summary>
/// 숫자를 조합하는 노드 - 최대 3개 입력을 받아 계산
/// </summary>
[NodeName("수학 계산 노드")]
[NodeCategory("테스트")]
[NodeDescription("최대 3개의 숫자를 입력받아 다양한 계산을 수행합니다.")]
public class MathCombinerNode : NodeBase, IResettable
{
    private enum OperationType
    {
        Add,
        Multiply,
        Min,
        Max,
        Average
    }

    private OperationType _operation = OperationType.Add;
    private int _lastResult;
    private int _accumulatedResult;
    private bool _useAccumulation = false;
    private bool _debugMode = true;

    [JsonConstructor]
    public MathCombinerNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Math Combiner";
        
        Input1Port = CreateInputPort<int>("Input 1");
        Input2Port = CreateInputPort<int>("Input 2");
        Input3Port = CreateInputPort<int>("Input 3");
        OperationPort = CreateInputPort<int>("Operation");
        AccumulatePort = CreateInputPort<bool>("Accumulate");
        ResultPort = CreateOutputPort<int>("Result");
        AccumulatedResultPort = CreateOutputPort<int>("Accumulated");
        HasResultPort = CreateOutputPort<bool>("Has Result");
    }

    public InputPort<int> Input1Port { get; }
    public InputPort<int> Input2Port { get; }
    public InputPort<int> Input3Port { get; }
    public InputPort<int> OperationPort { get; }
    public InputPort<bool> AccumulatePort { get; }
    public OutputPort<int> ResultPort { get; }
    public OutputPort<int> AccumulatedResultPort { get; }
    public OutputPort<bool> HasResultPort { get; }

    public int LastResult => _lastResult;
    public int AccumulatedResult => _accumulatedResult;

    public void Reset()
    {
        _lastResult = 0;
        _accumulatedResult = 0;
        
        if (_debugMode)
        {
            Console.WriteLine($"MathCombinerNode: Reset - lastResult={_lastResult}, accumulatedResult={_accumulatedResult}");
        }
    }

    public void SetUseAccumulation(bool useAccumulation)
    {
        _useAccumulation = useAccumulation;
        
        if (_debugMode)
        {
            Console.WriteLine($"MathCombinerNode: SetUseAccumulation - useAccumulation={_useAccumulation}");
        }
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        // 연산 타입 결정 (기본값은 Add)
        // 외부에서 _operation이 명시적으로 설정된 경우에는 OperationPort 값을 무시합니다
        // 이 부분이 테스트에서 리플렉션으로 설정한 값을 덮어쓰는 문제를 해결합니다
        OperationType originalOperation = _operation; // 원래 설정된 값 저장
        
        if (OperationPort.IsConnected)
        {
            int opValue = OperationPort.GetValueOrDefault(0);
            _operation = (OperationType)Math.Min(opValue, 4); // 0-4 범위로 제한
        }
        else
        {
            // OperationPort가 연결되지 않은 경우 원래 설정된 값 유지
            _operation = originalOperation;
        }

        // 누적 계산 여부 결정
        bool accumulate = AccumulatePort.GetValueOrDefault(_useAccumulation);

        // 입력값 가져오기 (연결되지 않은 포트는 0으로 처리)
        int input1 = Input1Port.GetValueOrDefault(0);
        int input2 = Input2Port.GetValueOrDefault(0);
        int input3 = Input3Port.GetValueOrDefault(0);

        if (_debugMode)
        {
            Console.WriteLine($"MathCombinerNode: operation={_operation}, inputs=[{input1}, {input2}, {input3}], accumulate={accumulate}, isConnected={Input1Port.IsConnected}");
        }

        // 결과 계산
        _lastResult = CalculateResult(input1, input2, input3);
        
        // 누적 계산 처리
        if (accumulate)
        {
            int prevAccumulated = _accumulatedResult;
            
            _accumulatedResult = _operation switch
            {
                OperationType.Add => _accumulatedResult + _lastResult,
                OperationType.Multiply => _accumulatedResult == 0 ? _lastResult : _accumulatedResult * _lastResult,
                OperationType.Min => _accumulatedResult == 0 ? _lastResult : Math.Min(_accumulatedResult, _lastResult),
                OperationType.Max => Math.Max(_accumulatedResult, _lastResult),
                OperationType.Average => _accumulatedResult == 0 ? _lastResult : (_accumulatedResult + _lastResult) / 2,
                _ => _accumulatedResult + _lastResult
            };
            
            if (_debugMode)
            {
                Console.WriteLine($"MathCombinerNode: Accumulating - lastResult={_lastResult}, prev={prevAccumulated}, new={_accumulatedResult}, input1={input1}, isConnected={Input1Port.IsConnected}");
            }
        }
        else
        {
            _accumulatedResult = _lastResult;
            
            if (_debugMode)
            {
                Console.WriteLine($"MathCombinerNode: Not accumulating - result={_lastResult}, input1={input1}, isConnected={Input1Port.IsConnected}");
            }
        }
        
        // 결과 출력
        ResultPort.Value = _lastResult;
        AccumulatedResultPort.Value = _accumulatedResult;
        HasResultPort.Value = true;
        
        if (_debugMode)
        {
            Console.WriteLine($"MathCombinerNode: Output - result={_lastResult}, accumulated={_accumulatedResult}, input1={input1}, isConnected={Input1Port.IsConnected}");
        }
    }

    private int CalculateResult(int input1, int input2, int input3)
    {
        int result = _operation switch
        {
            OperationType.Add => input1 + input2 + input3,
            OperationType.Multiply => input1 * input2 * input3,
            OperationType.Min => Math.Min(Math.Min(input1, input2), input3),
            OperationType.Max => Math.Max(Math.Max(input1, input2), input3),
            OperationType.Average => (input1 + input2 + input3) / 3,
            _ => input1 + input2 + input3
        };
        
        if (_debugMode)
        {
            Console.WriteLine($"MathCombinerNode: Calculation - operation={_operation}, inputs=[{input1}, {input2}, {input3}], result={result}");
        }
        
        return result;
    }
}

/// <summary>
/// 분기 노드 - 조건에 따라 다른 출력 포트로 데이터를 전달
/// </summary>
[NodeName("분기 노드")]
[NodeCategory("테스트")]
[NodeDescription("조건에 따라 입력값을 다른 출력 포트로 전달합니다.")]
public class BranchNode : NodeBase
{
    private Func<int, bool> _conditionFunc;
    private bool _debugMode = true;

    [JsonConstructor]
    public BranchNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Branch";
        
        ValuePort = CreateInputPort<int>("Value");
        ConditionPort = CreateInputPort<bool>("Condition");
        TruePort = CreateOutputPort<int>("True Branch");
        FalsePort = CreateOutputPort<int>("False Branch");

        // 기본 조건 함수는 짝수 확인
        _conditionFunc = value => value % 2 == 0;
    }

    public InputPort<int> ValuePort { get; }
    public InputPort<bool> ConditionPort { get; }
    public OutputPort<int> TruePort { get; }
    public OutputPort<int> FalsePort { get; }

    // 조건 함수 속성
    public Func<int, bool> ConditionFunction
    {
        get => _conditionFunc;
        set => _conditionFunc = value ?? (x => x % 2 == 0);
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        int value = ValuePort.GetValueOrDefault(0);
        bool useCustomCondition = ConditionPort.GetValueOrDefault(true);

        // 조건 평가 - 조건 함수가 true를 반환하는 경우 TruePort로 전달
        bool condition = _conditionFunc(value);
        
        if (_debugMode)
        {
            Console.WriteLine($"BranchNode: input={value}, condition={condition}, useCustomCondition={useCustomCondition}");
        }

        if (condition)
        {
            TruePort.Value = value;
            if (_debugMode) Console.WriteLine($"BranchNode: TruePort={value}");
        }
        else
        {
            FalsePort.Value = value;
            if (_debugMode) Console.WriteLine($"BranchNode: FalsePort={value}");
        }
    }
}

/// <summary>
/// 지연 노드 - 데이터를 일정 시간 지연시킨 후 출력
/// </summary>
[NodeName("지연 노드")]
[NodeCategory("테스트")]
[NodeDescription("입력값을 지정된 시간(밀리초) 동안 지연시킨 후 출력합니다.")]
public class DelayNode : NodeBase
{
    [JsonConstructor]
    public DelayNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Delay";
        
        ValuePort = CreateInputPort<int>("Value");
        DelayMsPort = CreateInputPort<int>("Delay (ms)");
        ResultPort = CreateOutputPort<int>("Result");
        CompletePort = CreateOutputPort<bool>("Complete");
    }

    public InputPort<int> ValuePort { get; }
    public InputPort<int> DelayMsPort { get; }
    public OutputPort<int> ResultPort { get; }
    public OutputPort<bool> CompletePort { get; }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        int value = ValuePort.GetValueOrDefault(0);
        int delayMs = DelayMsPort.GetValueOrDefault(100);
        
        // 최소 10ms, 최대 1000ms로 제한
        delayMs = Math.Min(Math.Max(delayMs, 10), 1000);
        
        // 지연 시간 적용
        await Task.Delay(delayMs, cancellationToken);
        
        // 결과 출력
        ResultPort.Value = value;
        CompletePort.Value = true;
    }
}

/// <summary>
/// 메모리 노드 - 이전 실행의 값을 저장하고 반환
/// </summary>
[NodeName("메모리 노드")]
[NodeCategory("테스트")]
[NodeDescription("이전 실행의 값을 저장하고 다음 실행에서 사용합니다.")]
public class MemoryNode : NodeBase, IResettable
{
    private readonly Queue<int> _memory = new Queue<int>();
    private int _capacity = 3;

    [JsonConstructor]
    public MemoryNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Memory";
        
        InputPort = CreateInputPort<int>("Input");
        CapacityPort = CreateInputPort<int>("Capacity");
        LatestValuePort = CreateOutputPort<int>("Latest Value");
        OldestValuePort = CreateOutputPort<int>("Oldest Value");
        AllValuesPort = CreateOutputPort<IEnumerable<int>>("All Values");
    }

    public InputPort<int> InputPort { get; }
    public InputPort<int> CapacityPort { get; }
    public OutputPort<int> LatestValuePort { get; }
    public OutputPort<int> OldestValuePort { get; }
    public OutputPort<IEnumerable<int>> AllValuesPort { get; }

    public IReadOnlyCollection<int> Memory => _memory;

    public void Reset()
    {
        _memory.Clear();
        _capacity = 3;
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        // 용량 설정 (기본값 3, 최소 1, 최대 10)
        int capacity = CapacityPort.GetValueOrDefault(3);
        _capacity = Math.Min(Math.Max(capacity, 1), 10);
        
        // 새 값 추가
        int newValue = InputPort.GetValueOrDefault(0);
        _memory.Enqueue(newValue);
        
        // 용량 초과 시 가장 오래된 값 제거
        while (_memory.Count > _capacity)
        {
            _memory.Dequeue();
        }
        
        // 결과 출력
        LatestValuePort.Value = _memory.LastOrDefault();
        OldestValuePort.Value = _memory.FirstOrDefault();
        AllValuesPort.Value = _memory.ToArray();
    }
}

/// <summary>
/// 병합 노드 - 여러 입력을 받아 하나의 출력으로 병합
/// </summary>
[NodeName("병합 노드")]
[NodeCategory("테스트")]
[NodeDescription("여러 입력을 받아 하나의 출력으로 병합합니다.")]
public class MergeNode : NodeBase, IResettable
{
    private readonly List<int> _values = new();
    private readonly Dictionary<int, Dictionary<string, int>> _cycleValues = new();
    private bool _debugMode = true;
    private bool _isFirstExecution = true;

    [JsonConstructor]
    public MergeNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Merge";
        
        Input1Port = CreateInputPort<int>("Input 1");
        Input2Port = CreateInputPort<int>("Input 2");
        Input3Port = CreateInputPort<int>("Input 3");
        IsCompletePort = CreateInputPort<bool>("IsComplete");
        
        ResultPort = CreateOutputPort<int>("Result");
        AllValuesPort = CreateOutputPort<IEnumerable<int>>("All Values");
        IsCompleteOutPort = CreateOutputPort<bool>("IsComplete");
    }

    public InputPort<int> Input1Port { get; }
    public InputPort<int> Input2Port { get; }
    public InputPort<int> Input3Port { get; }
    public InputPort<bool> IsCompletePort { get; }
    
    public OutputPort<int> ResultPort { get; }
    public OutputPort<IEnumerable<int>> AllValuesPort { get; }
    public OutputPort<bool> IsCompleteOutPort { get; }

    public IReadOnlyList<int> Values => _values;

    public void Reset()
    {
        _values.Clear();
        _cycleValues.Clear();
        _isFirstExecution = true;
        
        if (_debugMode)
        {
            Console.WriteLine("MergeNode: Reset - values cleared");
        }
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        // 현재 실행 사이클 가져오기 (ExecutionContext에서 가져올 수 있다면 좋겠지만, 여기서는 간단히 구현)
        int currentCycle = 0;
        
        if (_debugMode)
        {
            Console.WriteLine($"MergeNode: Processing in cycle {currentCycle}");
        }
        
        // 현재 사이클의 값 저장소 초기화
        if (!_cycleValues.TryGetValue(currentCycle, out var cycleData))
        {
            cycleData = new Dictionary<string, int>();
            _cycleValues[currentCycle] = cycleData;
        }
        
        // 각 입력 포트에서 값 가져오기 및 저장
        if (Input1Port.IsConnected)
        {
            int value = Input1Port.GetValueOrDefault(0);
            cycleData["Input1"] = value;
            if (_debugMode) Console.WriteLine($"MergeNode: Stored Input1 value: {value} for cycle {currentCycle}");
        }
        
        if (Input2Port.IsConnected)
        {
            int value = Input2Port.GetValueOrDefault(0);
            cycleData["Input2"] = value;
            if (_debugMode) Console.WriteLine($"MergeNode: Stored Input2 value: {value} for cycle {currentCycle}");
        }
        
        if (Input3Port.IsConnected)
        {
            int value = Input3Port.GetValueOrDefault(0);
            cycleData["Input3"] = value;
            if (_debugMode) Console.WriteLine($"MergeNode: Stored Input3 value: {value} for cycle {currentCycle}");
        }
        
        // 완료 신호 처리
        bool isComplete = IsCompletePort.GetValueOrDefault(false);
        
        if (_debugMode)
        {
            Console.WriteLine($"MergeNode: IsComplete signal: {isComplete}");
        }
        
        // 백프레셔 패턴: 모든 연결된 입력 포트에서 값을 받았는지 확인
        bool allInputsReady = true;
        
        // 첫 번째 실행 시에는 모든 입력이 준비되지 않았을 수 있으므로 건너뜀
        if (_isFirstExecution)
        {
            _isFirstExecution = false;
            
            // 첫 실행에서도 값이 있으면 처리
            if (cycleData.Count > 0)
            {
                allInputsReady = true;
            }
            else
            {
                allInputsReady = false;
                if (_debugMode) Console.WriteLine("MergeNode: First execution, waiting for all inputs");
            }
        }
        else
        {
            // 연결된 입력 포트 확인
            if (Input1Port.IsConnected && !cycleData.ContainsKey("Input1"))
            {
                // 값이 없지만 이전에 값을 받은 적이 있으면 계속 진행
                if (_values.Any())
                {
                    if (_debugMode) Console.WriteLine("MergeNode: Input1 not ready but continuing with previous values");
                }
                else
                {
                    allInputsReady = false;
                    if (_debugMode) Console.WriteLine("MergeNode: Input1 not ready yet");
                }
            }
            
            if (Input2Port.IsConnected && !cycleData.ContainsKey("Input2"))
            {
                // 값이 없지만 이전에 값을 받은 적이 있으면 계속 진행
                if (_values.Any())
                {
                    if (_debugMode) Console.WriteLine("MergeNode: Input2 not ready but continuing with previous values");
                }
                else
                {
                    allInputsReady = false;
                    if (_debugMode) Console.WriteLine("MergeNode: Input2 not ready yet");
                }
            }
            
            if (Input3Port.IsConnected && !cycleData.ContainsKey("Input3"))
            {
                // 값이 없지만 이전에 값을 받은 적이 있으면 계속 진행
                if (_values.Any())
                {
                    if (_debugMode) Console.WriteLine("MergeNode: Input3 not ready but continuing with previous values");
                }
                else
                {
                    allInputsReady = false;
                    if (_debugMode) Console.WriteLine("MergeNode: Input3 not ready yet");
                }
            }
        }
        
        // 모든 입력이 준비되었거나 완료 신호를 받았을 때 결과 출력
        if (allInputsReady || isComplete)
        {
            // 현재 사이클의 모든 값을 _values에 추가
            foreach (var value in cycleData.Values)
            {
                _values.Add(value);
                if (_debugMode) Console.WriteLine($"MergeNode: Added value to results: {value}");
            }
            
            // 결과 출력
            if (cycleData.Values.Any())
            {
                int sum = cycleData.Values.Sum(); // 현재 사이클의 모든 값의 합
                ResultPort.Value = sum;
                
                if (_debugMode)
                {
                    Console.WriteLine($"MergeNode: Output - sum={sum}, all values=[{string.Join(", ", _values)}]");
                }
            }
            else if (_values.Any() && isComplete)
            {
                // 완료 신호를 받았고 이전에 수집된 값이 있는 경우
                // 테스트에서는 누적된 값의 합을 기대하므로 모든 값의 합을 출력
                
                // 마지막 값들을 가져옴 (각 입력 포트별로 마지막 값)
                Dictionary<string, int> lastValues = new Dictionary<string, int>();
                
                // 입력 포트별로 마지막 값 찾기
                if (Input1Port.IsConnected)
                {
                    lastValues["Input1"] = Input1Port.GetValueOrDefault(0);
                    if (_debugMode) Console.WriteLine($"MergeNode: Last value from Input1: {lastValues["Input1"]}");
                }
                
                if (Input2Port.IsConnected)
                {
                    lastValues["Input2"] = Input2Port.GetValueOrDefault(0);
                    if (_debugMode) Console.WriteLine($"MergeNode: Last value from Input2: {lastValues["Input2"]}");
                }
                
                if (Input3Port.IsConnected)
                {
                    lastValues["Input3"] = Input3Port.GetValueOrDefault(0);
                    if (_debugMode) Console.WriteLine($"MergeNode: Last value from Input3: {lastValues["Input3"]}");
                }
                
                int sum = lastValues.Values.Sum();
                ResultPort.Value = sum;
                
                if (_debugMode)
                {
                    Console.WriteLine($"MergeNode: Output from last values on completion - sum={sum}");
                }
            }
            
            // 모든 수집된 값을 출력
            AllValuesPort.Value = _values.ToArray();
            
            // 사이클 데이터 정리 (다음 사이클을 위해)
            _cycleValues.Remove(currentCycle);
        }
        else
        {
            if (_debugMode)
            {
                Console.WriteLine("MergeNode: Not all inputs are ready, waiting for more data");
            }
        }
        
        IsCompleteOutPort.Value = isComplete;
        
        await Task.CompletedTask;
    }
} 