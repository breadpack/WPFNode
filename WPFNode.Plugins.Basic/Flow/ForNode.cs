using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

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
    
    /// <summary>
    /// 현재 루프 반복 횟수
    /// </summary>
    public int CurrentIteration { get; private set; } = 0;
    
    /// <summary>
    /// 시작 인덱스
    /// </summary>
    [NodeProperty("시작 인덱스")]
    public NodeProperty<int> StartIndex { get; private set; }
    
    /// <summary>
    /// 종료 인덱스
    /// </summary>
    [NodeProperty("종료 인덱스")]
    public NodeProperty<int> EndIndex { get; private set; }
    
    /// <summary>
    /// 증가/감소 단계
    /// </summary>
    [NodeProperty("증가/감소 단계")]
    public NodeProperty<int> Step { get; private set; }
    
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
        // NodeProperty 속성 초기화 및 기본값 설정
        // CreateProperty 대신에 직접 생성하여 초기화
        StartIndex = new NodeProperty<int>(nameof(StartIndex), "시작 인덱스", this, 1);
        EndIndex = new NodeProperty<int>(nameof(EndIndex), "종료 인덱스", this, 2);
        Step = new NodeProperty<int>(nameof(Step), "증가/감소 단계", this, 3);
        
        // 기본값 설정
        StartIndex.Value = 0;
        EndIndex.Value = 10;
        Step.Value = 1;
    }
    
    // 테스트에서 사용하는 생성자 추가
    public ForNode(INodeCanvas canvas, Guid id) 
        : this(canvas, id, null)
    {
    }
    
    /// <summary>
    /// 노드의 처리 로직을 구현합니다.
    /// 루프 실행 및 흐름 제어를 담당합니다.
    /// </summary>
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        FlowExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        Logger?.LogDebug("Executing ForNode: {StartIndex} to {EndIndex} step {Step}", 
            StartIndex.Value, EndIndex.Value, Step.Value);
        
        // 첫 실행 시 초기화
        InitializeLoop();
        
        // 현재 인덱스를 출력 포트에 설정
        CurrentIndex.Value = _currentIndex;
        
        // 루프 계속 실행 여부 확인 - 조건과 최대 반복 횟수 체크
        for(int i = StartIndex.Value; Step.Value < 0 ? i >= EndIndex.Value : i <= EndIndex.Value; i += Step.Value)
        {
            // 루프 반복 횟수 증가
            CurrentIteration++;
            
            // 현재 인덱스 업데이트
            _currentIndex = i;
            CurrentIndex.Value = _currentIndex;
            
            // 루프 본문 실행
            yield return LoopBody;
        }

        // 완료 포트 반환
        yield return LoopComplete;
    }

    /// <summary>
    /// 루프 반복을 위한 상태를 초기화합니다.
    /// </summary>
    private void InitializeLoop()
    {
        _currentIndex = StartIndex.Value;
        CurrentIteration = 0;
        
        // Step이 0이면 경고 로그 및 기본값 1로 설정
        if (Step.Value == 0)
        {
            Logger?.LogWarning("ForNode: Step is 0, setting to default value 1 to prevent infinite loop");
            Step.Value = 1;
        }
        
        // 디버그 로그 추가
        Logger?.LogDebug("ForNode: Loop initialized with StartIndex={Start}, EndIndex={End}, Step={Step}", 
            StartIndex.Value, EndIndex.Value, Step.Value);
    }
}
