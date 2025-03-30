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
public class ConstantNode : NodeBase
{
    [NodeProperty("Type", OnValueChanged = nameof(OnTypeChanged))]
    public NodeProperty<Type> Type { get; private set; }

    private INodeProperty _valueProperty;
    private IOutputPort   _outputPort;

    public ConstantNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) { }

    protected override void Configure(NodeBuilder builder) {
        if (Type.Value == null) {
            return;
        }

        _valueProperty = builder.Property("Value", "Value", Type.Value);
        _outputPort    = builder.Output("Result", Type.Value);
    }
    
    private void OnTypeChanged() {
        ReconfigurePorts();
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        if(Type.Value == null) {
            _outputPort.Value = null;
            yield break;
        }
        
        // Value.Value에서 값을 가져와 Result.Value에 설정
        Debug.WriteLine($"ConstantNode.ProcessAsync: Value={_valueProperty.Value}, Type={Type.Value}");
        _outputPort.Value = _valueProperty.Value;
    }
} 