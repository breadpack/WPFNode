using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Object.IsNull")]
[NodeCategory("객체")]
[NodeDescription("객체가 null인지 확인합니다.")]
public class IsNullNode : NodeBase
{
    [NodeInput("객체")]
    public InputPort<object> Input { get; set; }
    
    [NodeOutput("Null 여부")]
    public OutputPort<bool> Result { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }
    
    private bool _resultValue = false;
    public bool ResultValue 
    {
        get => _resultValue;
        set 
        {
            _resultValue = value;
            if (Result != null)
                Result.Value = value;
        }
    }

    public IsNullNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        // 입력 객체 가져오기
        object input = Input?.GetValueOrDefault(null);
        
        // null 체크 수행
        bool isNull = input == null;
        
        // 결과 설정
        ResultValue = isNull;
        
        // 필요한 비동기 작업을 처리하기 위한 대기
        await Task.CompletedTask;
        
        // FlowOut이 있으면 반환
        if (FlowOut != null)
        {
            yield return FlowOut;
        }
    }
}
