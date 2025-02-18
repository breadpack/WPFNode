using System.Text.Json.Serialization;
using WPFNode.Attributes;
using WPFNode.Interfaces;

namespace WPFNode.Models;

[OutputNode]
public class GraphOutputNode<T> : NodeBase
{
    private readonly InputPort<T> _input;
    private readonly OutputPort<T> _parentOutput;

    [JsonConstructor]
    public GraphOutputNode(INodeCanvas canvas, Guid id) 
        : base(canvas, id)
    {
        _input = CreateInputPort<T>("Value");
        // 직렬화 시에는 _parentOutput이 null이 될 수 있음
        _parentOutput = null!;
    }

    public GraphOutputNode(INodeCanvas canvas, Guid id, OutputPort<T> parentOutput) 
        : base(canvas, id)
    {
        _parentOutput = parentOutput;
        _input = CreateInputPort<T>("Value");
    }

    public InputPort<T> Input => _input;

    public override async Task ProcessAsync()
    {
        if (_parentOutput != null)
        {
            _parentOutput.Value = _input.GetValueOrDefault();
        }
        await Task.CompletedTask;
    }
} 