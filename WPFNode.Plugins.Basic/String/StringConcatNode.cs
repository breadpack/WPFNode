using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.String;

[NodeName("String.Concat")]
[NodeCategory("문자열")]
[NodeDescription("여러 문자열을 하나로 연결합니다.")]
public class StringConcatNode : NodeBase
{
    [NodeInput("문자열 A")]
    public InputPort<string> InputA { get; set; }
    
    [NodeInput("문자열 B")]
    public InputPort<string> InputB { get; set; }
    
    [NodeInput("문자열 C (옵션)")]
    public InputPort<string> InputC { get; set; }
    
    [NodeOutput("결과")]
    public OutputPort<string> Result { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeProperty("구분자", CanConnectToPort = true)]
    public NodeProperty<string> Separator { get; set; }
    
    [NodeProperty("구분자 사용", CanConnectToPort = false)]
    public NodeProperty<bool> UseSeparator { get; set; }
    
    [NodeProperty("C 사용", CanConnectToPort = false)]
    public NodeProperty<bool> UseInputC { get; set; }

    public StringConcatNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        Separator.Value = "";
        UseSeparator.Value = false;
        UseInputC.Value = false;
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        // 입력 문자열 가져오기
        string inputA = InputA?.GetValueOrDefault(string.Empty) ?? string.Empty;
        string inputB = InputB?.GetValueOrDefault(string.Empty) ?? string.Empty;
        string inputC = UseInputC.Value ? (InputC?.GetValueOrDefault(string.Empty) ?? string.Empty) : string.Empty;
        
        string result;
        
        // 구분자 사용 여부에 따라 연결 방식 결정
        if (UseSeparator.Value)
        {
            string separator = Separator?.Value ?? string.Empty;
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(inputA))
                parts.Add(inputA);
                
            if (!string.IsNullOrEmpty(inputB))
                parts.Add(inputB);
                
            if (UseInputC.Value && !string.IsNullOrEmpty(inputC))
                parts.Add(inputC);
            
            result = string.Join(separator, parts);
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append(inputA);
            sb.Append(inputB);
            
            if (UseInputC.Value)
                sb.Append(inputC);
                
            result = sb.ToString();
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
