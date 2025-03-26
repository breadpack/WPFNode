using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;

namespace WPFNode.Demo.Nodes
{
    [NodeCategory("변수")]
    [NodeName("Int 변수")]
    [NodeDescription("정수 값을 저장하고 출력합니다.")]
    public class IntVariableNode : NodeBase
    {
        [NodeInput]
        public InputPort<int> ValueInput { get; private set; }

        [NodeOutput]
        public OutputPort<int> ValueOutput { get; private set; }

        [NodeProperty]
        public NodeProperty<string> VariableName { get; private set; }

        [NodeProperty]
        public NodeProperty<int> DefaultValue { get; private set; }

        public IntVariableNode(INodeCanvas canvas, Guid guid = default)
            : base(canvas, guid == default ? Guid.NewGuid() : guid)
        {
        }

        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var defaultVal = DefaultValue?.Value ?? 0;
            // 입력 값을 출력 포트로 전달
            ValueOutput.Value = ValueInput.GetValueOrDefault(defaultVal);
            
            // 이 노드는 플로우 포트가 없으므로 비어있는 열거형 반환
            yield break;
        }
    }
}
