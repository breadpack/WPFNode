using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WPFNode.Plugins.Basic.String;

[NodeName("String.ToUpper")]
[NodeCategory("문자열")]
[NodeDescription("문자열을 대문자로 변환합니다.")]
public class StringToUpperNode : NodeBase
{
    [NodeInput("입력 문자열")]
    public InputPort<string> Input { get; set; }
    
    [NodeOutput("결과")]
    public OutputPort<string> Result { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }

    public StringToUpperNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
    }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 입력 문자열 가져오기
        string input = Input?.GetValueOrDefault(string.Empty);
        
        // 대문자 변환 수행
        string result = input?.ToUpper() ?? string.Empty;
        
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
