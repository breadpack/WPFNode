using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Plugins.Basic.Variables;

/// <summary>
/// 값을 저장하고 변경할 수 있는 변수 노드입니다.
/// </summary>
/// <typeparam name="T">변수의 데이터 타입</typeparam>
[NodeName("Variable")]
[NodeCategory("Variables")]
[NodeDescription("값을 저장하고 변경할 수 있습니다.")]
public class VariableNode<T> : NodeBase
{
    /// <summary>
    /// 변수 값 입력 포트
    /// </summary>
    [NodeInput("Value")]
    public InputPort<T> Value { get; private set; }

    /// <summary>
    /// 변수 값 출력 포트
    /// </summary>
    [NodeOutput("Value")]
    public OutputPort<T> Output { get; private set; }

    /// <summary>
    /// 변수 이름 속성
    /// </summary>
    [NodeProperty("Name")]
    public string VariableName { get; set; } = "Variable";

    public VariableNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
    }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(CancellationToken cancellationToken = default) {
        // 입력 값을 출력 포트로 전달
        Output.Value = Value.GetValueOrDefault();
        yield break;
    }
} 