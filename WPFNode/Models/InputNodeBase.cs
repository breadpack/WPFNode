using WPFNode.Attributes;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Models;

public abstract class InputNodeBase<T> : NodeBase
{
    [NodeFlowIn]
    public IFlowInPort InPort { get; private set; }
    
    [NodeFlowOut]
    public IFlowOutPort OutPort { get; private set; }
    
    protected readonly OutputPort<T> _output;
    
    public OutputPort<T>   Result        => _output;
    
    public NodeProperty<T> InputProperty { get; }

    protected InputNodeBase(INodeCanvas canvas, Guid guid) : base(canvas, guid) 
    {
        _output = CreateOutputPort<T>("Value");
        
        // AddProperty를 사용하여 Value 속성 추가
        InputProperty = CreateProperty<T>(
            "Value",
            "Value");
    }


    public T Value
    {
        get => InputProperty.Value!;
        set => InputProperty.Value = value;
    }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken) {
        // Value 속성의 값이 이미 OutputPort에 연결되어 있으므로
        // 추가 작업 필요 없음
        Result.Value = Value;
        yield return OutPort;
    }
} 