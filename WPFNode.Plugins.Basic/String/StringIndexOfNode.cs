using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.String;

[NodeName("String.IndexOf")]
[NodeCategory("문자열")]
[NodeDescription("문자열에서 지정된 문자 또는 부분 문자열이 처음 나타나는 위치를 찾습니다.")]
public class StringIndexOfNode : NodeBase
{
    [NodeInput("입력 문자열")]
    public InputPort<string> Input { get; set; }
    
    [NodeProperty("검색할 문자열", CanConnectToPort = true)]
    public NodeProperty<string> Value { get; set; }
    
    [NodeProperty("시작 인덱스", CanConnectToPort = true)]
    public NodeProperty<int> StartIndex { get; set; }
    
    [NodeOutput("인덱스")]
    public OutputPort<int> Result { get; set; }
    
    [NodeOutput("찾음")]
    public OutputPort<bool> Found { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeProperty("대소문자 구분 안함", CanConnectToPort = false)]
    public NodeProperty<bool> IgnoreCase { get; set; }
    
    [NodeProperty("시작 인덱스 사용", CanConnectToPort = false)]
    public NodeProperty<bool> UseStartIndex { get; set; }

    public StringIndexOfNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        IgnoreCase.Value = false;
        UseStartIndex.Value = false;
        Value.Value = "";
        StartIndex.Value = 0;
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        FlowExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        // 입력 문자열 가져오기
        string input = Input?.GetValueOrDefault(string.Empty) ?? string.Empty;
        string value = Value?.Value ?? string.Empty;
        int startIndex = UseStartIndex.Value ? (StartIndex?.Value ?? 0) : 0;
        
        // 잘못된 시작 인덱스 검사
        if (startIndex < 0)
            startIndex = 0;
        else if (startIndex > input.Length)
            startIndex = input.Length;
        
        int index = -1;
        bool found = false;
        
        // value가 비어있지 않은 경우에만 IndexOf 수행
        if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(input))
        {
            try
            {
                if (IgnoreCase.Value)
                {
                    // 대소문자 구분 없이 검색
                    StringComparison comparison = StringComparison.OrdinalIgnoreCase;
                    
                    if (UseStartIndex.Value)
                        index = input.IndexOf(value, startIndex, comparison);
                    else
                        index = input.IndexOf(value, comparison);
                }
                else
                {
                    // 기본 IndexOf 호출 (대소문자 구분)
                    if (UseStartIndex.Value)
                        index = input.IndexOf(value, startIndex);
                    else
                        index = input.IndexOf(value);
                }
                
                found = index >= 0;
            }
            catch
            {
                // 오류 발생 시 기본값 유지
                index = -1;
                found = false;
            }
        }
        
        // 결과 설정
        if (Result != null)
            Result.Value = index;
            
        if (Found != null)
            Found.Value = found;
        
        // 필요한 비동기 작업을 처리하기 위한 대기
        await Task.CompletedTask;
        
        // FlowOut이 있으면 반환
        if (FlowOut != null)
        {
            yield return FlowOut;
        }
    }
}
