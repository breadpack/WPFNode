using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WPFNode.Plugins.Basic.String;

[NodeName("String.Replace")]
[NodeCategory("문자열")]
[NodeDescription("문자열 내에서 지정된 부분 문자열을 다른 문자열로 대체합니다.")]
public class StringReplaceNode : NodeBase
{
    [NodeInput("입력 문자열")]
    public InputPort<string> Input { get; set; }
    
    [NodeInput("검색할 문자열")]
    public InputPort<string> OldValue { get; set; }
    
    [NodeInput("대체할 문자열")]
    public InputPort<string> NewValue { get; set; }
    
    [NodeOutput("결과")]
    public OutputPort<string> Result { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }

    public StringReplaceNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
    }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 입력 문자열 가져오기
        string input = Input?.GetValueOrDefault(string.Empty) ?? string.Empty;
        string oldValue = OldValue?.GetValueOrDefault(string.Empty) ?? string.Empty;
        string newValue = NewValue?.GetValueOrDefault(string.Empty) ?? string.Empty;
        
        string result = input;
        
        // oldValue가 비어있지 않은 경우에만 Replace 수행
        if (!string.IsNullOrEmpty(oldValue))
        {
            result = input.Replace(oldValue, newValue);
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
