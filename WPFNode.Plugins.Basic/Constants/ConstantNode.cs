using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.Constants;

/// <summary>
/// 상수 값을 출력하는 노드입니다.
/// </summary>
/// <typeparam name="T">상수의 데이터 타입</typeparam>
[NodeCategory("Constants")]
[NodeDescription("상수 값을 출력합니다.")]
public class ConstantNode<T> : NodeBase
{
    [NodeProperty("Value")]
    public NodeProperty<T> Value { get; private set; }

    /// <summary>
    /// 상수 값 출력 포트
    /// </summary>
    [NodeOutput("Result")]
    public OutputPort<T> Result { get; private set; }

    public ConstantNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) { }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        FlowExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        // Value.Value에서 값을 가져와 Result.Value에 설정
        Debug.WriteLine($"ConstantNode.ProcessAsync: Value={Value}, Type={typeof(T).Name}");
        
        if (Value != null && Result != null)
        {
            Result.Value = Value.Value;
            Debug.WriteLine($"ConstantNode.ProcessAsync: 값 설정 완료, Result 값이 설정됨");
        }
        else
        {
            Debug.WriteLine($"ConstantNode.ProcessAsync: Value 또는 Result가 null입니다. Value={Value != null}, Result={Result != null}");
        }
        
        // 필요한 비동기 작업을 처리하기 위한 대기
        yield break;
    }
} 