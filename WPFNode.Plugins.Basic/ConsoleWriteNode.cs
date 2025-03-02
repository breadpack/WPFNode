using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Plugins.Basic;

[NodeName("Console Write")]
[NodeCategory("Basic")]
[NodeDescription("콘솔에 문자열을 출력하는 노드입니다.")]
[OutputNode]
public class ConsoleWriteNode : NodeBase {
    private readonly InputPort<string> _input;
    
    public InputPort<string> Input => _input;

    public ConsoleWriteNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        _input = CreateInputPort<string>("Text");
    }

    protected override Task ProcessAsync(CancellationToken cancellationToken = default) {
        Console.WriteLine(_input.GetValueOrDefault(string.Empty));
        return Task.CompletedTask;
    }
}