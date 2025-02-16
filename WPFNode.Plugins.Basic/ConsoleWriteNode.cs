using WPFNode.Abstractions;
using WPFNode.Abstractions.Attributes;
using WPFNode.Core.Models;

namespace WPFNode.Plugins.Basic;

[NodeName("Console Write")]
[NodeCategory("Basic")]
[NodeDescription("콘솔에 문자열을 출력하는 노드입니다.")]
[OutputNode]
public class ConsoleWriteNode : NodeBase {
    private readonly InputPort<string> _input;
    
    public InputPort<string> Input => _input;

    public ConsoleWriteNode(INodeCanvas canvas) : base(canvas) {
        _input = CreateInputPort<string>("Text");
    }

    public override Task ProcessAsync() {
        Console.WriteLine(_input.Value);
        return Task.CompletedTask;
    }
}