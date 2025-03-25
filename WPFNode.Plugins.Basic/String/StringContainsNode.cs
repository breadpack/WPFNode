using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WPFNode.Plugins.Basic.String;

[NodeName("String.Contains")]
[NodeCategory("문자열")]
[NodeDescription("문자열이 지정된 부분 문자열을 포함하는지 확인합니다.")]
public class StringContainsNode : NodeBase
{
    [NodeInput("입력 문자열")]
    public InputPort<string> Input { get; set; }
    
    [NodeProperty("검색할 문자열", CanConnectToPort = true)]
    public NodeProperty<string> Value { get; set; }
    
    [NodeOutput("포함 여부")]
    public OutputPort<bool> Result { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeProperty("대소문자 구분 안함", CanConnectToPort = false)]
    public NodeProperty<bool> IgnoreCase { get; set; }

    public StringContainsNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        IgnoreCase.Value = false;
        Value.Value = "";
    }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 입력 문자열 가져오기
        string input = Input?.GetValueOrDefault(string.Empty) ?? string.Empty;
        string value = Value?.Value ?? string.Empty;
        
        bool result = false;
        
        // value가 비어있지 않은 경우에만 Contains 수행
        if (!string.IsNullOrEmpty(value))
        {
            if (IgnoreCase.Value)
            {
                // 대소문자 구분 없이 검색
                result = input.ToLower().Contains(value.ToLower());
            }
            else
            {
                // 기본 Contains 호출 (대소문자 구분)
                result = input.Contains(value);
            }
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
