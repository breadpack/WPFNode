using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.String;

[NodeName("String.Length")]
[NodeCategory("문자열")]
[NodeDescription("문자열의 길이를 계산합니다.")]
public class StringLengthNode : NodeBase
{
    [NodeInput("입력 문자열")]
    public InputPort<string> Input { get; set; }
    
    [NodeOutput("길이")]
    public OutputPort<int> Length { get; set; }
    
    [NodeOutput("문자열이 비어있음")]
    public OutputPort<bool> IsEmpty { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }

    public StringLengthNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        FlowExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        // 입력 문자열 가져오기
        string input = Input?.GetValueOrDefault(string.Empty) ?? string.Empty;
        
        // 문자열 길이 계산
        int length = input.Length;
        bool isEmpty = length == 0;
        
        // 결과 설정
        if (Length != null)
            Length.Value = length;
            
        if (IsEmpty != null)
            IsEmpty.Value = isEmpty;
        
        // 필요한 비동기 작업을 처리하기 위한 대기
        await Task.CompletedTask;
        
        // FlowOut이 있으면 반환
        if (FlowOut != null)
        {
            yield return FlowOut;
        }
    }
}
