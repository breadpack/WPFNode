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

    public ConsoleWriteNode(INodeCanvas canvas, Guid id) : base(canvas, id) {
        _input = CreateInputPort<string>("Text");
    }

    public override Task ProcessAsync() {
        Console.WriteLine(_input.Value);
        return Task.CompletedTask;
    }
}