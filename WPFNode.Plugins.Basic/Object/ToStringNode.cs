using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Object.ToString")]
[NodeCategory("객체")]
[NodeDescription("객체를 문자열로 변환합니다.")]
public class ToStringNode : NodeBase
{
    [NodeInput("객체")]
    public InputPort<object> Input { get; set; }
    
    [NodeOutput("문자열")]
    public OutputPort<string> Result { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeProperty("형식", CanConnectToPort = true)]
    public NodeProperty<string> Format { get; set; }
    
    [NodeProperty("형식 사용", CanConnectToPort = false)]
    public NodeProperty<bool> UseFormat { get; set; }
    
    [NodeProperty("Null 객체 표시", CanConnectToPort = false)]
    public NodeProperty<string> NullRepresentation { get; set; }

    public ToStringNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        UseFormat.Value = false;
        Format.Value = "";
        NullRepresentation.Value = "(null)";
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        // 입력 객체 가져오기
        object input = Input?.GetValueOrDefault(null);
        
        string result;
        
        // 객체가 null인 경우
        if (input == null)
        {
            result = NullRepresentation.Value;
        }
        else
        {
            try
            {
                // 형식 사용 여부에 따라 호출 방식 결정
                if (UseFormat.Value && !string.IsNullOrEmpty(Format.Value))
                {
                    // IFormattable 인터페이스 지원 확인
                    if (input is IFormattable formattable)
                    {
                        result = formattable.ToString(Format.Value, null);
                    }
                    else
                    {
                        // IFormattable이 아닌 경우 일반 ToString 호출
                        result = input.ToString();
                    }
                }
                else
                {
                    // 기본 ToString 호출
                    result = input.ToString();
                }
            }
            catch
            {
                // 예외 발생 시 타입 이름 반환
                result = $"[{input.GetType().Name}]";
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
