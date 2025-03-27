using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic;

[NodeName("Console Write")]
[NodeCategory("Basic")]
[NodeDescription("콘솔에 문자열을 출력하는 노드입니다.")]
[OutputNode]
public class ConsoleWriteNode : NodeBase {
    [NodeFlowIn]
    public IFlowInPort InPort { get; set; }
    
    [NodeFlowOut]
    public IFlowOutPort OutPort { get; set; }
    
    [NodeInput("Text")]
    public InputPort<string> Input { get; set; }

    public ConsoleWriteNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) { }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken) {
        Console.WriteLine(Input.GetValueOrDefault(string.Empty));
        yield return OutPort;
    }
}