using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Dictionary.Create")]
[NodeDescription("새 Dictionary를 생성합니다.")]
[NodeCategory("컬렉션")]
public class DictionaryCreateNode : NodeBase
{
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; set; }
    
    [NodeFlowOut]
    public IFlowOutPort FlowOut { get; set; }
    
    [NodeProperty("키 타입", OnValueChanged = nameof(TypeChanged))]
    public NodeProperty<Type> KeyType { get; set; }
    
    [NodeProperty("값 타입", OnValueChanged = nameof(TypeChanged))]
    public NodeProperty<Type> ValueType { get; set; }
    
    private IOutputPort _dictionaryOutput;
    
    public DictionaryCreateNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Dictionary.Create";
        Description = "새 Dictionary를 생성합니다.";
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
        
        _dictionaryOutput = builder.Output("Dictionary", dictType);
    }
    
    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken cancellationToken = default)
    {
        var keyType = KeyType?.Value ?? typeof(object);
        var valueType = ValueType?.Value ?? typeof(object);
        var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        
        // 새 Dictionary 생성
        var dictionary = Activator.CreateInstance(dictType);
        
        // 출력 포트에 설정
        ((dynamic)_dictionaryOutput).Value = dictionary;
        
        yield return FlowOut;
    }
} 