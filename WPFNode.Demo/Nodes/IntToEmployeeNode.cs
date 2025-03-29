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
    [NodeName("Int -> Employee 변환")]
    [NodeDescription("정수 ID를 Employee 객체로 변환합니다. (암시적 변환 테스트)")]
    public class IntToEmployeeNode : NodeBase
    {
        [NodeFlowIn("실행")]
        public FlowInPort FlowIn { get; private set; }

        [NodeFlowOut("완료")]
        public FlowOutPort FlowOut { get; private set; }

        [NodeInput]
        public InputPort<int> IdInput { get; private set; }

        [NodeOutput]
        public OutputPort<Employee> EmployeeOutput { get; private set; }

        public IntToEmployeeNode(INodeCanvas canvas, Guid guid = default)
            : base(canvas, guid == default ? Guid.NewGuid() : guid)
        {
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken)
        {
            var id = IdInput.GetValueOrDefault(0);
            // 암시적 변환 연산자를 통해 int -> Employee 변환
            EmployeeOutput.Value = id;
            
            // FlowOut 포트 반환 (실행 흐름 계속)
            yield return FlowOut;
        }
    }
}
