using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.String;

[NodeName("String.IsNullOrEmpty")]
[NodeCategory("문자열")]
[NodeDescription("문자열이 null이거나 빈 문자열인지 확인합니다.")]
public class StringIsNullOrEmptyNode : NodeBase
{
    [NodeInput("입력 문자열")]
    public InputPort<string> Input { get; set; }
    
    [NodeOutput("결과")]
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

    public StringIsNullOrEmptyNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        // 입력 문자열 가져오기
        string input = Input?.GetValueOrDefault(null);
        
        // string.IsNullOrEmpty 검사 수행
        bool result = string.IsNullOrEmpty(input);
        
        // 결과 설정
        ResultValue = result;
        
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
