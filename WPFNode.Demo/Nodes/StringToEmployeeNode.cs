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
    [NodeName("JSON -> Employee 변환")]
    [NodeDescription("JSON 문자열을 Employee 객체로 변환합니다. (생성자 테스트)")]
    public class StringToEmployeeNode : NodeBase
    {
        [NodeFlowIn("실행")]
        public FlowInPort FlowIn { get; private set; }

        [NodeFlowOut("완료")]
        public FlowOutPort FlowOut { get; private set; }

        [NodeInput]
        public InputPort<string> JsonInput { get; private set; }

        [NodeOutput]
        public OutputPort<Employee> EmployeeOutput { get; private set; }

        public StringToEmployeeNode(INodeCanvas canvas, Guid guid = default)
            : base(canvas, guid == default ? Guid.NewGuid() : guid)
        {
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(IExecutionContext? context, CancellationToken cancellationToken)
        {
            var json = JsonInput.GetValueOrDefault("{}");
            
            // 생성자를 통한 변환
            EmployeeOutput.Value = new Employee(json);
            
            // FlowOut 포트 반환 (실행 흐름 계속)
            yield return FlowOut;
        }
    }
}
