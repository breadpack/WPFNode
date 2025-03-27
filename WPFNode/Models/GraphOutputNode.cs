using System.Text.Json.Serialization;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models.Execution;

namespace WPFNode.Models;

[OutputNode]
public class GraphOutputNode<T> : NodeBase
{
    [NodeFlowOut]
    public IFlowOutPort FlowOut { get; private set; }
    
    private readonly InputPort<T> _input;
    private readonly OutputPort<T> _parentOutput;

    [JsonConstructor]
    public GraphOutputNode(INodeCanvas canvas, Guid guid) 
        : base(canvas, guid)
    {
        _input = CreateInputPort<T>("Value");
        // 직렬화 시에는 _parentOutput이 null이 될 수 있음
        _parentOutput = null!;
    }

    public GraphOutputNode(INodeCanvas canvas, Guid guid, OutputPort<T> parentOutput) 
        : base(canvas, guid)
    {
        _parentOutput = parentOutput;
        _input = CreateInputPort<T>("Value");
    }

    public InputPort<T> Input => _input;

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken) {
        if (_parentOutput != null)
        {
            _parentOutput.Value = _input.GetValueOrDefault();
        }
        yield return FlowOut;
    }
} 