using System.Text.Json.Serialization;
using WPFNode.Interfaces;
using WPFNode.Models.Execution;

namespace WPFNode.Models;

public class GraphInputNode<T> : NodeBase
{
    private readonly OutputPort<T> _output;
    private readonly InputPort<T> _parentInput;

    [JsonConstructor]
    public GraphInputNode(INodeCanvas canvas, Guid guid) 
        : base(canvas, guid)
    {
        _output = CreateOutputPort<T>("Value");
        // 직렬화 시에는 _parentInput이 null이 될 수 있음
        _parentInput = null!;
    }

    public GraphInputNode(INodeCanvas canvas, Guid guid, InputPort<T> parentInput) 
        : base(canvas, guid)
    {
        _parentInput = parentInput;
        _output = CreateOutputPort<T>("Value");
    }

    public OutputPort<T> Output => _output;
    
    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(IExecutionContext? context, CancellationToken cancellationToken) {
        if (_parentInput != null)
        {
            _output.Value = _parentInput.GetValueOrDefault();
        }

        yield break;
    }
} 