using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.Flow;

/// <summary>
/// 실행 흐름의 시작점 역할을 하는 노드입니다.
/// </summary>
[NodeName("Start")]
[NodeCategory("Flow Control")]
[NodeDescription("실행 흐름의 시작점입니다.")]
public class StartNode : NodeBase, IFlowEntry
{
    /// <summary>
    /// 실행 흐름 출력 포트
    /// </summary>
    [NodeFlowOut("Out")]
    public FlowOutPort FlowOut { get; private set; }

    public StartNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(IExecutionContext? context, CancellationToken cancellationToken) {
        // Start 노드는 특별한 처리 없이 다음 노드로 실행 흐름을 전달합니다.
        yield return FlowOut;
    }
} 