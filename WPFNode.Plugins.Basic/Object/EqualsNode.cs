using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Object.Equals")]
[NodeCategory("객체")]
[NodeDescription("두 객체가 동일한지 비교합니다.")]
public class EqualsNode : NodeBase
{
    [NodeInput("객체 A")]
    public InputPort<object> InputA { get; set; }
    
    [NodeProperty("객체 B", CanConnectToPort = true)]
    public NodeProperty<object> InputB { get; set; }
    
    [NodeOutput("동일 여부")]
    public OutputPort<bool> Result { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeProperty("참조 동일성만 비교", CanConnectToPort = false)]
    public NodeProperty<bool> ReferenceEquals { get; set; }

    public EqualsNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        ReferenceEquals.Value = false;
        InputB.Value = null;
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        FlowExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        // 입력 객체 가져오기
        object objectA = InputA?.GetValueOrDefault(null);
        object objectB = InputB?.Value;
        
        bool result;
        
        // 비교 방식에 따라 다른 메서드 호출
        if (ReferenceEquals.Value)
        {
            // 참조 동일성 비교
            result = System.Object.ReferenceEquals(objectA, objectB);
        }
        else
        {
            // 객체 동일성 비교 (Equals 메서드 사용)
            if (objectA == null)
                result = objectB == null;
            else
                result = objectA.Equals(objectB);
        }
        
        // 결과 설정
        if (Result != null)
            Result.Value = result;
        
        // 필요한 비동기 작업을 처리하기 위한 대기
        await Task.CompletedTask;
        
        // FlowOut이 있으면 반환
        if (FlowOut != null)
        {
            yield return FlowOut;
        }
    }
}
