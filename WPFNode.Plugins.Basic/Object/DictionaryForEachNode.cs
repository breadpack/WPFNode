using System;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Dictionary.ForEach")]
[NodeDescription("Dictionary의 모든 키-값 쌍을 순회합니다.")]
[NodeCategory("컬렉션")]
public class DictionaryForEachNode : NodeBase
{
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; set; }
    
    [NodeFlowOut("Item")]
    public IFlowOutPort ItemOut { get; set; }
    
    [NodeFlowOut("Complete")]
    public IFlowOutPort CompleteOut { get; set; }
    
    [NodeProperty("Key Type", OnValueChanged = nameof(TypeChanged))]
    public NodeProperty<Type> KeyType { get; set; }
    
    [NodeProperty("Value Type", OnValueChanged = nameof(TypeChanged))]
    public NodeProperty<Type> ValueType { get; set; }
    
    private IInputPort _dictionaryInput;
    private IOutputPort _currentKeyOutput;
    private IOutputPort _currentValueOutput;
    private IOutputPort _indexOutput;
    
    public DictionaryForEachNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Dictionary.ForEach";
        Description = "Dictionary의 모든 키-값 쌍을 순회합니다.";
    }
    
    private void TypeChanged()
    {
        ReconfigurePorts();
    }
    
    protected override void Configure(NodeBuilder builder)
    {
        var keyType = KeyType?.Value ?? typeof(object);
        var valueType = ValueType?.Value ?? typeof(object);
        var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        
        _dictionaryInput = builder.Input("Dictionary", dictType);
        _currentKeyOutput = builder.Output("CurrentKey", keyType);
        _currentValueOutput = builder.Output("CurrentValue", valueType);
        _indexOutput = builder.Output("Index", typeof(int));
    }
    
    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken cancellationToken = default)
    {
        var dictionary = ((dynamic)_dictionaryInput).GetValueOrDefault();
        
        if (dictionary == null)
        {
            yield return CompleteOut;
            yield break;
        }
        
        int index = 0;
        
        // Dictionary의 각 항목을 순회
        foreach (var entry in dictionary)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // 현재 항목 정보 설정
            ((dynamic)_currentKeyOutput).Value = entry.Key;
            ((dynamic)_currentValueOutput).Value = entry.Value;
            ((dynamic)_indexOutput).Value = index++;
            
            // 항목 처리 Flow 출력
            yield return ItemOut;
        }
        
        // 순회 완료
        yield return CompleteOut;
    }
} 