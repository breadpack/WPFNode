using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Interfaces;
using WPFNode.Interfaces.Flow;
using WPFNode.Models.Execution;

namespace WPFNode.Models.Flow;

/// <summary>
/// 흐름 포트를 지원하는 노드의 기본 클래스입니다.
/// </summary>
public abstract class FlowNodeBase : NodeBase
{
    private readonly List<IFlowInPort> _flowInPorts = new();
    private readonly List<IFlowOutPort> _flowOutPorts = new();

    /// <summary>
    /// 흐름 노드 생성자
    /// </summary>
    /// <param name="canvas">노드 캔버스</param>
    /// <param name="id">노드 ID</param>
    protected FlowNodeBase(INodeCanvas canvas, Guid id) : base(canvas, id)
    {
    }

    /// <summary>
    /// 흐름 입력 포트 목록
    /// </summary>
    public IReadOnlyCollection<IFlowInPort> FlowInPorts => _flowInPorts.AsReadOnly();

    /// <summary>
    /// 흐름 출력 포트 목록
    /// </summary>
    public IReadOnlyCollection<IFlowOutPort> FlowOutPorts => _flowOutPorts.AsReadOnly();

    /// <summary>
    /// 흐름 입력 포트 추가
    /// </summary>
    /// <param name="name">포트 이름</param>
    /// <returns>추가된 흐름 입력 포트</returns>
    protected IFlowInPort AddFlowInPort(string name)
    {
        var port = new FlowInPort(this, name);
        _flowInPorts.Add(port);
        return port;
    }

    /// <summary>
    /// 흐름 출력 포트 추가
    /// </summary>
    /// <param name="name">포트 이름</param>
    /// <returns>추가된 흐름 출력 포트</returns>
    protected IFlowOutPort AddFlowOutPort(string name)
    {
        var port = new FlowOutPort(this, name);
        _flowOutPorts.Add(port);
        return port;
    }

    /// <summary>
    /// 노드 실행 메서드 재정의
    /// </summary>
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // 부모 클래스의 실행 메서드 호출 (데이터 계산 등 수행)
        await base.ExecuteAsync(cancellationToken);
        
        // 실행 후 추가 로직을 여기서 구현할 수 있음
    }
    
    /// <summary>
    /// 노드 실행 후 흐름 전파
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    public virtual async Task PropagateFlowAsync(WPFNode.Models.Execution.ExecutionContext context, CancellationToken cancellationToken)
    {
        // 기본 구현: 첫 번째 흐름 출력 포트로만 전파
        if (_flowOutPorts.Count > 0)
        {
            await _flowOutPorts[0].PropagateFlowAsync(context, cancellationToken);
        }
    }
}
