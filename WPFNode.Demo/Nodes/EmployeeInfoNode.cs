using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using WPFNode.Demo.Models;

namespace WPFNode.Demo.Nodes
{
    [NodeCategory("출력")]
    [NodeName("Employee 정보")]
    [NodeDescription("Employee 객체의 정보를 표시합니다.")]
    public class EmployeeInfoNode : NodeBase
    {
        [NodeFlowIn("실행")]
        public FlowInPort FlowIn { get; private set; }

        [NodeFlowOut("완료")]
        public FlowOutPort FlowOut { get; private set; }

        [NodeInput]
        public InputPort<Employee> EmployeeInput { get; private set; }

        [NodeProperty]
        public NodeProperty<int> Id { get; private set; }

        [NodeProperty]
        public NodeProperty<string> Name { get; private set; }

        [NodeProperty]
        public NodeProperty<string> Department { get; private set; }

        [NodeProperty]
        public NodeProperty<decimal> Salary { get; private set; }

        public EmployeeInfoNode(INodeCanvas canvas, Guid guid = default)
            : base(canvas, guid == default ? Guid.NewGuid() : guid)
        {
        }

        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var employee = EmployeeInput.GetValueOrDefault(new Employee());
            
            Id.Value = employee.Id;
            Name.Value = employee.Name;
            Department.Value = employee.Department;
            Salary.Value = employee.Salary;
            
            // FlowOut 포트 반환 (실행 흐름 계속)
            yield return FlowOut;
        }
    }
}
