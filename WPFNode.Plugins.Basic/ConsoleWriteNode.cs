using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Plugins.Basic;

[NodeName("Console Write")]
[NodeCategory("Basic")]
[NodeDescription("콘솔에 문자열을 출력하는 노드입니다.")]
[OutputNode]
public class ConsoleWriteNode : NodeBase {
    [NodeInput("Text")]
    public InputPort<string> Input { get; set; }

    public ConsoleWriteNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) { }

    protected override Task ProcessAsync(CancellationToken cancellationToken = default) {
        Console.WriteLine(Input.GetValueOrDefault(string.Empty));
        return Task.CompletedTask;
    }
}