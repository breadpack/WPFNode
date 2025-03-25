using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WPFNode.Plugins.Basic.String;

[NodeName("String.EndsWith")]
[NodeCategory("문자열")]
[NodeDescription("문자열이 지정된 부분 문자열로 끝나는지 확인합니다.")]
public class StringEndsWithNode : NodeBase
{
    [NodeInput("입력 문자열")]
    public InputPort<string> Input { get; set; }
    
    [NodeInput("끝 문자열")]
    public InputPort<string> Value { get; set; }
    
    [NodeOutput("결과")]
    public OutputPort<bool> Result { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeProperty("대소문자 구분 안함", CanConnectToPort = false)]
    public NodeProperty<bool> IgnoreCase { get; set; }

    public StringEndsWithNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        IgnoreCase.Value = false;
    }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 입력 문자열 가져오기
        string input = Input?.GetValueOrDefault(string.Empty) ?? string.Empty;
        string value = Value?.GetValueOrDefault(string.Empty) ?? string.Empty;
        
        bool result = false;
        
        // value가 비어있지 않은 경우에만 EndsWith 수행
        if (!string.IsNullOrEmpty(value))
        {
            if (IgnoreCase.Value)
            {
                // 대소문자 구분 없이 비교
                StringComparison comparison = StringComparison.OrdinalIgnoreCase;
                result = input.EndsWith(value, comparison);
            }
            else
            {
                // 기본 EndsWith 호출 (대소문자 구분)
                result = input.EndsWith(value);
            }
        }
        else
        {
            // 끝 문자열이 비어있으면 모든 문자열이 빈 문자열로 끝난다고 간주
            result = true;
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
