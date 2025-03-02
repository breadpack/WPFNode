using System.Text.Json.Serialization;
using WPFNode.Interfaces;

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

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        if (_parentInput != null)
        {
            _output.Value = _parentInput.GetValueOrDefault();
        }
        await Task.CompletedTask;
    }
} 