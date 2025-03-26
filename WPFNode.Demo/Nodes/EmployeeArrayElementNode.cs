using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Demo.Models;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;

namespace WPFNode.Demo.Nodes;


[NodeCategory("변환")]
[NodeName("Employee 배열 요소")]
[NodeDescription("Employee 배열의 요소를 가져옵니다.")]
public class EmployeeArrayElementNode : NodeBase {
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; private set; }
    
    [NodeFlowOut]
    public IFlowOutPort FlowOut { get; private set; }
    
    [NodeProperty]
    public NodeProperty<IEmployee[]> EmployeeArrayInput { get; private set; }
    
    [NodeProperty]
    public NodeProperty<int> Index { get; private set; }
    
    [NodeOutput]
    public OutputPort<IEmployee> EmployeeOutput { get; private set; }
    
    
    public EmployeeArrayElementNode(INodeCanvas                                      canvas, Guid guid) : base(canvas, guid) { }
    
    protected override IAsyncEnumerable<IFlowOutPort> ProcessAsync(CancellationToken cancellationToken = default) {
        throw new NotImplementedException();
    }
}