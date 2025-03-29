using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.String;

[NodeName("String.Substring")]
[NodeCategory("문자열")]
[NodeDescription("문자열의 지정된 위치에서 지정된 길이만큼의 부분 문자열을 반환합니다.")]
public class StringSubstringNode : NodeBase
{
    [NodeInput("입력 문자열")]
    public InputPort<string> Input { get; set; }
    
    [NodeProperty("시작 인덱스", CanConnectToPort = true)]
    public NodeProperty<int> StartIndex { get; set; }
    
    [NodeProperty("길이", CanConnectToPort = true)]
    public NodeProperty<int> Length { get; set; }
    
    [NodeOutput("결과")]
    public OutputPort<string> Result { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeProperty("길이 옵션 사용", CanConnectToPort = false)]
    public NodeProperty<bool> UseLengthOption { get; set; }

    public StringSubstringNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        UseLengthOption.Value = false;
        StartIndex.Value = 0;
        Length.Value = 0;
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        FlowExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        string result = string.Empty;
        
        // 입력 문자열 가져오기
        string input = Input?.GetValueOrDefault(string.Empty) ?? string.Empty;
        int startIndex = StartIndex?.Value ?? 0;
        
        // 빈 문자열 또는 유효하지 않은 인덱스 체크
        if (!string.IsNullOrEmpty(input) && startIndex < input.Length)
        {
            try
            {
                // 길이 옵션 사용 여부에 따라 Substring 호출 방식 결정
                if (UseLengthOption.Value)
                {
                    int length = Length?.Value ?? (input.Length - startIndex);
                    
                    // 유효한 범위 확인
                    if (startIndex + length > input.Length)
                        length = input.Length - startIndex;
                    
                    result = input.Substring(startIndex, length);
                }
                else
                {
                    result = input.Substring(startIndex);
                }
            }
            catch (Exception ex)
            {
                // 오류 발생시 로그 기록하고 빈 문자열 유지
                Debug.WriteLine($"Substring 연산 중 오류 발생: {ex.Message}");
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
