using System.Text.Json.Serialization;
using WPFNode.Interfaces;

namespace WPFNode.Models;

public class GraphInputNode<T> : NodeBase
{
    private readonly OutputPort<T> _output;
    private readonly InputPort<T> _parentInput;

    [JsonConstructor]
    public GraphInputNode(INodeCanvas canvas, Guid id) 
        : base(canvas, id)
    {
        _output = CreateOutputPort<T>("Value");
        // 직렬화 시에는 _parentInput이 null이 될 수 있음
        _parentInput = null!;
    }

    public GraphInputNode(INodeCanvas canvas, Guid id, InputPort<T> parentInput) 
        : base(canvas, id)
    {
        _parentInput = parentInput;
        _output = CreateOutputPort<T>("Value");
    }

    public OutputPort<T> Output => _output;

    public override async Task ProcessAsync()
    {
        if (_parentInput != null)
        {
            _output.Value = _parentInput.GetValueOrDefault();
        }
        await Task.CompletedTask;
    }
} 