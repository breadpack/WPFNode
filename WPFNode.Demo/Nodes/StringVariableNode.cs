using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Demo.Nodes
{
    [NodeCategory("변수")]
    [NodeName("String 변수")]
    [NodeDescription("문자열 값을 저장하고 출력합니다.")]
    public class StringVariableNode : NodeBase
    {
        [NodeInput]
        public InputPort<string> ValueInput { get; private set; }

        [NodeOutput]
        public OutputPort<string> ValueOutput { get; private set; }

        [NodeProperty]
        public NodeProperty<string> VariableName { get; private set; }

        [NodeProperty]
        public NodeProperty<string> DefaultValue { get; private set; }

        public StringVariableNode(INodeCanvas canvas, Guid guid = default)
            : base(canvas, guid == default ? Guid.NewGuid() : guid)
        {
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(IExecutionContext? context, CancellationToken cancellationToken)
        {
            var defaultVal = DefaultValue?.Value ?? "";
            // 입력 값을 출력 포트로 전달
            ValueOutput.Value = ValueInput.GetValueOrDefault(defaultVal);
            
            // 이 노드는 플로우 포트가 없으므로 비어있는 열거형 반환
            yield break;
        }
    }
}
