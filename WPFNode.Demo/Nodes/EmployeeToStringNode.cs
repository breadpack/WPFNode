using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Demo.Models;
using WPFNode.Models.Execution;

namespace WPFNode.Demo.Nodes
{
    [NodeCategory("변환")]
    [NodeName("Employee -> 문자열 변환")]
    [NodeDescription("Employee 객체를 JSON 문자열로 변환합니다. (명시적 변환 테스트)")]
    public class EmployeeToStringNode : NodeBase
    {
        [NodeFlowIn("실행")]
        public FlowInPort FlowIn { get; private set; }

        [NodeFlowOut("완료")]
        public FlowOutPort FlowOut { get; private set; }

        [NodeInput]
        public InputPort<Employee> EmployeeInput { get; private set; }

        [NodeOutput]
        public OutputPort<string> JsonOutput { get; private set; }

        public EmployeeToStringNode(INodeCanvas canvas, Guid guid = default)
            : base(canvas, guid == default ? Guid.NewGuid() : guid)
        {
        }

        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken)
        {
            var employee = EmployeeInput.GetValueOrDefault(new Employee());
            
            // 명시적 변환 연산자를 사용
            JsonOutput.Value = (string)employee;
            
            // FlowOut 포트 반환 (실행 흐름 계속)
            yield return FlowOut;
        }
    }
}
