using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.String;

[NodeName("String.Split")]
[NodeCategory("문자열")]
[NodeDescription("문자열을 지정된 구분자로 분할합니다.")]
public class StringSplitNode : NodeBase
{
    [NodeInput("입력 문자열")]
    public InputPort<string> Input { get; set; }
    
    [NodeProperty("구분자", CanConnectToPort = true)]
    public NodeProperty<string> Separator { get; set; }
    
    [NodeOutput("결과 배열")]
    public OutputPort<string[]> ResultArray { get; set; }
    
    [NodeOutput("첫 번째 항목")]
    public OutputPort<string> FirstItem { get; set; }
    
    [NodeOutput("두 번째 항목")]
    public OutputPort<string> SecondItem { get; set; }
    
    [NodeOutput("마지막 항목")]
    public OutputPort<string> LastItem { get; set; }
    
    [NodeOutput("항목 개수")]
    public OutputPort<int> Count { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeProperty("공백 항목 제거", CanConnectToPort = false)]
    public NodeProperty<bool> RemoveEmptyEntries { get; set; }
    
    [NodeProperty("문자열 구분자 사용", CanConnectToPort = false)]
    public NodeProperty<bool> UseStringSeparator { get; set; }

    public StringSplitNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        RemoveEmptyEntries.Value = false;
        UseStringSeparator.Value = true;
        Separator.Value = ",";
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        FlowExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        // 입력 문자열 가져오기
        string input = Input?.GetValueOrDefault(string.Empty) ?? string.Empty;
        string separator = Separator?.Value ?? ",";
        
        // 빈 입력 검사
        if (string.IsNullOrEmpty(input))
        {
            // 빈 배열 결과 설정
            string[] emptyResult = new string[0];
            
            if (ResultArray != null)
                ResultArray.Value = emptyResult;
            
            if (FirstItem != null)
                FirstItem.Value = string.Empty;
                
            if (SecondItem != null)
                SecondItem.Value = string.Empty;
                
            if (LastItem != null)
                LastItem.Value = string.Empty;
                
            if (Count != null)
                Count.Value = 0;
                
            // FlowOut 반환
            if (FlowOut != null)
                yield return FlowOut;
                
            yield break;
        }
        
        // 분할 옵션 설정
        StringSplitOptions options = RemoveEmptyEntries.Value 
            ? StringSplitOptions.RemoveEmptyEntries 
            : StringSplitOptions.None;
            
        string[] result;
        
        // 구분자 사용 방식에 따라 Split 호출
        if (UseStringSeparator.Value)
        {
            // 문자열 구분자 사용 (여러 문자가 하나의 구분자로 취급됨)
            result = input.Split(new string[] { separator }, options);
        }
        else
        {
            // 문자 배열 구분자 사용 (각 문자가 별도의 구분자로 취급됨)
            result = input.Split(separator.ToCharArray(), options);
        }
        
        // 결과 설정
        if (ResultArray != null)
            ResultArray.Value = result;
            
        // 첫 번째, 두 번째, 마지막 항목 설정
        if (FirstItem != null)
            FirstItem.Value = result.Length > 0 ? result[0] : string.Empty;
            
        if (SecondItem != null)
            SecondItem.Value = result.Length > 1 ? result[1] : string.Empty;
            
        if (LastItem != null)
            LastItem.Value = result.Length > 0 ? result[result.Length - 1] : string.Empty;
            
        // 항목 개수 설정
        if (Count != null)
            Count.Value = result.Length;
        
        // 필요한 비동기 작업을 처리하기 위한 대기
        await Task.CompletedTask;
        
        // FlowOut이 있으면 반환
        if (FlowOut != null)
        {
            yield return FlowOut;
        }
    }
}
