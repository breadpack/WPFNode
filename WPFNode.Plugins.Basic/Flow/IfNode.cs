using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.Flow;

/// <summary>
/// 조건에 따라 실행 경로를 분기하는 If 노드입니다.
/// </summary>
[NodeCategory("Flow Control")]
[NodeName("If")]
[NodeDescription("조건에 따라 실행 경로를 분기합니다.")]
public class IfNode : NodeBase
{
    /// <summary>
    /// 조건 (입력)
    /// </summary>
    [NodeInput("Condition")]
    public InputPort<bool> Condition { get; private set; }
    
    /// <summary>
    /// If 노드 진입 Flow 포트
    /// </summary>
    [NodeFlowIn("Enter")]
    public FlowInPort FlowIn { get; private set; }
    
    /// <summary>
    /// 조건이 참일 때 실행 Flow 포트
    /// </summary>
    [NodeFlowOut("True")]
    public FlowOutPort TruePort { get; private set; }
    
    /// <summary>
    /// 조건이 거짓일 때 실행 Flow 포트
    /// </summary>
    [NodeFlowOut("False")]
    public FlowOutPort FalsePort { get; private set; }
    
    public IfNode(INodeCanvas canvas, Guid id, ILogger? logger = null) 
        : base(canvas, id, logger)
    {
    }
    
    // 테스트에서 사용하는 생성자 추가
    public IfNode(INodeCanvas canvas, Guid id) 
        : this(canvas, id, null)
    {
    }
    
    /// <summary>
    /// 노드의 처리 로직을 구현합니다.
    /// 조건에 따라 True 또는 False 포트를 반환합니다.
    /// </summary>
    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        // 조건 평가
        bool condition = Condition.GetValueOrDefault(false);
        
        Logger?.LogDebug("IfNode: Condition evaluated to {Condition}", condition);
        
        // 필요한 비동기 작업 처리
        await Task.CompletedTask;
        
        // 조건에 따라 적절한 FlowOutPort 반환
        if (condition)
        {
            yield return TruePort;
        }
        else
        {
            yield return FalsePort;
        }
    }
}
