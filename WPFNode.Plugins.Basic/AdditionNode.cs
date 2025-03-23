using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace WPFNode.Plugins.Basic;

[NodeName("덧셈")]
[NodeCategory("기본 연산")]
[NodeDescription("두 수를 더합니다.")]
public class AdditionNode : NodeBase
{
    [NodeInput("A")]
    public InputPort<double>  InputA { get; set; }
    
    [NodeInput("B")]
    public InputPort<double>  InputB { get; set; }
    
    [NodeOutput("Output")]
    public OutputPort<double> Result { get; set; }

    [NodeFlowIn("Execute")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("Out")]
    public FlowOutPort FlowOut { get; set; }

    // Result 값이 직접 접근 가능하도록 별도의 속성 추가
    private double _resultValue = 0.0;
    public double ResultValue 
    {
        get => _resultValue;
        set 
        {
            _resultValue = value;
            if (Result != null)
                Result.Value = value;
        }
    }

    public AdditionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
    }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // InputPort에서 값을 가져옵니다
        double a = InputA?.GetValueOrDefault(0.0) ?? 0.0;
        double b = InputB?.GetValueOrDefault(0.0) ?? 0.0;
        
        Debug.WriteLine($"AdditionNode.ProcessAsync: a={a}, b={b}");
        
        // 결과 계산
        double result = a + b;
        
        // 결과 설정 - Result.Value와 ResultValue 모두 설정
        ResultValue = result;
        
        if (Result != null)
            Result.Value = result;
        
        Debug.WriteLine($"AdditionNode.ProcessAsync: result={result}, ResultValue={ResultValue}");
        
        // 필요한 비동기 작업을 처리하기 위한 대기
        await Task.CompletedTask;
        
        // FlowOut이 있으면 반환
        if (FlowOut != null)
        {
            yield return FlowOut;
        }
    }
} 