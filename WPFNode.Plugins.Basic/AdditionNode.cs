using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Plugins.Basic;

[NodeName("덧셈")]
[NodeCategory("기본 연산")]
[NodeDescription("두 수를 더합니다.")]
public class AdditionNode : NodeBase
{
    [NodeInput("A")]
    public InputPort<double>  InputA { get; set; }
    
    [NodeInput("B")]
    public InputPort<double>  InputB { get; set; }
    
    [NodeOutput("Output")]
    public OutputPort<double> Result { get; set; }

    public AdditionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var a = InputA.GetValueOrDefault(0.0);
        var b = InputB.GetValueOrDefault(0.0);
        Result.Value = a + b;
        await Task.CompletedTask;
    }
} 