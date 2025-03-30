using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.Flow;

/// <summary>
/// 조건이 참인 동안 반복 실행하는 While 루프 노드입니다.
/// </summary>
[NodeCategory("Flow Control")]
[NodeName("While Loop")]
[NodeDescription("조건이 참인 동안 반복 실행합니다.")]
public class WhileNode : NodeBase
{
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
    /// 루프 조건 (입력)
    /// </summary>
    [NodeInput("Condition")]
    public InputPort<bool> Condition { get; private set; }
    
    /// <summary>
    /// 현재 반복 횟수 (출력)
    /// </summary>
    [NodeOutput("Iterations")]
    public OutputPort<int> Iterations { get; private set; }
    
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
    
    public WhileNode(INodeCanvas canvas, Guid id) 
        : base(canvas, id)
    {
    }
    
    /// <summary>
    /// 노드의 처리 로직을 구현합니다.
    /// </summary>
    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(IExecutionContext? context, CancellationToken cancellationToken) {
        Logger?.LogDebug("Executing WhileNode");
        
        // 초기화
        InitializeLoop();
        
        // 현재 반복 횟수를 출력 포트에 설정
        Iterations.Value = CurrentIteration;
        
        // 표준 while 루프로 구현
        while (Condition.GetValueOrDefault(false) && CurrentIteration < MaxIterations) 
        {
            Logger?.LogDebug("WhileNode: Iteration {Iteration}, Condition = true", CurrentIteration);
            
            // 루프 본문 실행
            yield return LoopBody;
            
            // 반복 상태 업데이트
            CurrentIteration++;
            Iterations.Value = CurrentIteration;
            
            // 취소 요청 확인
            if (cancellationToken.IsCancellationRequested)
                break;
        }
        
        // 최대 반복 횟수 초과 시 경고 로그
        if (CurrentIteration >= MaxIterations) {
            Logger?.LogWarning("WhileNode: Maximum iterations ({Max}) reached!", MaxIterations);
        }
        
        Logger?.LogDebug("WhileNode: Loop completed after {Iterations} iterations", CurrentIteration);
        
        yield return LoopComplete;
    }
    
    /// <summary>
    /// 루프 반복을 위한 상태를 초기화합니다.
    /// </summary>
    private void InitializeLoop()
    {
        CurrentIteration = 0;
        
        // 무한 루프 방지를 위한 기본값 설정
        if (MaxIterations <= 0)
            MaxIterations = DEFAULT_MAX_ITERATIONS;
    }
}
