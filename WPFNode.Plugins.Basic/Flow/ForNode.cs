using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.Flow;

/// <summary>
/// 지정된 범위 내에서 반복 실행하는 For 루프 노드입니다.
/// </summary>
[NodeCategory("Flow Control")]
[NodeName("For Loop")]
[NodeDescription("지정된 범위 내에서 반복 실행합니다.")]
public class ForNode : NodeBase
{
    private int _currentIndex = 0;
    private bool _initialized = false;
    private bool _returnToLoop = false;
    private readonly int DEFAULT_MAX_ITERATIONS = 1000;
    
    /// <summary>
    /// 현재 루프 반복 횟수
    /// </summary>
    public int CurrentIteration { get; private set; } = 0;
    
    /// <summary>
    /// 루프의 최대 반복 횟수
    /// </summary>
    [NodeProperty]
    public int MaxIterations { get; set; } = 1000;
    
    /// <summary>
    /// 시작 인덱스
    /// </summary>
    [NodeProperty]
    public int StartIndex { get; set; } = 0;
    
    /// <summary>
    /// 종료 인덱스
    /// </summary>
    [NodeProperty]
    public int EndIndex { get; set; } = 10;
    
    /// <summary>
    /// 증가/감소 단계
    /// </summary>
    [NodeProperty]
    public int Step { get; set; } = 1;
    
    /// <summary>
    /// 현재 인덱스 (출력)
    /// </summary>
    [NodeOutput("Current Index")]
    public OutputPort<int> CurrentIndex { get; private set; }
    
    /// <summary>
    /// 루프 진입 Flow 포트
    /// </summary>
    [NodeFlowIn("Enter")]
    public FlowInPort FlowIn { get; private set; }
    
    /// <summary>
    /// 루프 본문 실행 Flow 포트
    /// </summary>
    [NodeFlowOut("Body")]
    public FlowOutPort LoopBody { get; private set; }
    
    /// <summary>
    /// 루프 완료 Flow 포트
    /// </summary>
    [NodeFlowOut("Complete")]
    public FlowOutPort LoopComplete { get; private set; }
    
    public ForNode(INodeCanvas canvas, Guid id, ILogger? logger = null) 
        : base(canvas, id, logger)
    {
    }
    
    /// <summary>
    /// 노드의 처리 로직을 구현합니다.
    /// </summary>
    protected override Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        // 아무 작업도 하지 않음 - 실제 로직은 ExecuteAsync에서 처리
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 노드를 실행하고 다음에 실행할 FlowOutPort를 yield return으로 반환합니다.
    /// </summary>
    public override async IAsyncEnumerable<IFlowOutPort> ExecuteAsyncFlow(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Logger?.LogDebug("Executing ForNode: {StartIndex} to {EndIndex} step {Step}", 
            StartIndex, EndIndex, Step);
        
        // 첫 실행 또는 루프백 후 구분하여 처리
        if (!_initialized)
        {
            // 초기화
            InitializeLoop();
        }
        else if (_returnToLoop)
        {
            // 루프 본문 실행 후 돌아온 경우 상태 업데이트
            UpdateLoopState();
        }
        
        // 현재 인덱스를 출력 포트에 설정
        CurrentIndex.Value = _currentIndex;
        
        // 루프 조건 검사 및 최대 반복 횟수 확인
        if (ShouldContinueLoop() && CurrentIteration < MaxIterations)
        {
            Logger?.LogDebug("ForNode: Iteration {Iteration}, Index = {Index}", 
                CurrentIteration, _currentIndex);
            
            // 루프 본문 실행
            yield return LoopBody;
            
            // 다음 반복을 위해 플래그 설정
            _returnToLoop = true;
            
            // 다음 반복을 위해 자신을 다시 실행
            yield return this.LoopBack();
        }
        else
        {
            // 최대 반복 횟수 초과 시 경고 로그
            if (CurrentIteration >= MaxIterations)
            {
                Logger?.LogWarning("ForNode: Maximum iterations ({Max}) reached!", MaxIterations);
            }
            
            Logger?.LogDebug("ForNode: Loop completed after {Iterations} iterations", 
                CurrentIteration);
            
            // 루프 완료, 상태 초기화
            _initialized = false;
            _returnToLoop = false;
            
            yield return LoopComplete;
        }
    }
    
    /// <summary>
    /// 루프가 계속 실행되어야 하는지 확인합니다.
    /// </summary>
    private bool ShouldContinueLoop()
    {
        if (Step > 0)
            return _currentIndex <= EndIndex;
        else if (Step < 0)
            return _currentIndex >= EndIndex;
        else
            return false; // Step이 0이면 무한 루프 방지를 위해 false 반환
    }
    
    /// <summary>
    /// 루프 반복을 위한 상태를 초기화합니다.
    /// </summary>
    private void InitializeLoop()
    {
        _currentIndex = StartIndex;
        CurrentIteration = 0;
        _initialized = true;
        _returnToLoop = false;
        
        // 무한 루프 방지를 위한 기본값 설정
        if (MaxIterations <= 0)
            MaxIterations = DEFAULT_MAX_ITERATIONS;
            
        // Step이 0이면 경고 로그 및 기본값 1로 설정
        if (Step == 0)
        {
            Logger?.LogWarning("ForNode: Step is 0, setting to default value 1 to prevent infinite loop");
            Step = 1;
        }
    }
    
    /// <summary>
    /// 각 반복 후 상태를 업데이트합니다.
    /// </summary>
    private void UpdateLoopState()
    {
        _currentIndex += Step;
        CurrentIteration++;
        _returnToLoop = false;
    }
}
