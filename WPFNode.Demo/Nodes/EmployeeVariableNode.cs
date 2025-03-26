using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Demo.Models;

namespace WPFNode.Demo.Nodes
{
    [NodeCategory("변수")]
    [NodeName("Employee 변수")]
    [NodeDescription("Employee 객체를 저장하고 출력합니다.")]
    public class EmployeeVariableNode : NodeBase
    {
        [NodeInput]
        public InputPort<Employee> ValueInput { get; private set; }

        [NodeOutput]
        public OutputPort<Employee> ValueOutput { get; private set; }

        [NodeProperty]
        public string VariableName { get; set; } = "Employee";

        public EmployeeVariableNode(INodeCanvas canvas, Guid guid = default)
            : base(canvas, guid == default ? Guid.NewGuid() : guid)
        {
        }

        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // 입력 값을 출력 포트로 전달
            ValueOutput.Value = ValueInput.GetValueOrDefault(new Employee());
            
            // 이 노드는 플로우 포트가 없으므로 비어있는 열거형 반환
            yield break;
        }
    }
}
