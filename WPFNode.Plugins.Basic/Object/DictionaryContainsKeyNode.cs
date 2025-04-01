using System;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Dictionary.ContainsKey")]
[NodeDescription("Dictionary에 지정된 키가 존재하는지 확인합니다.")]
[NodeCategory("컬렉션")]
public class DictionaryContainsKeyNode : NodeBase
{
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; set; }
    
    [NodeFlowOut("존재")]
    public IFlowOutPort ExistsOut { get; set; }
    
    [NodeFlowOut("존재안함")]
    public IFlowOutPort NotExistsOut { get; set; }
    
    [NodeProperty("키 타입", OnValueChanged = nameof(TypeChanged))]
    public NodeProperty<Type> KeyType { get; set; }
    
    [NodeProperty("값 타입", OnValueChanged = nameof(TypeChanged))]
    public NodeProperty<Type> ValueType { get; set; }
    
    private IInputPort _dictionaryInput;
    private IInputPort _keyInput;
    private IOutputPort _keyExistsOutput;
    
    public DictionaryContainsKeyNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Dictionary.ContainsKey";
        Description = "Dictionary에 지정된 키가 존재하는지 확인합니다.";
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
        _keyInput = builder.Input("Key", keyType);
        _keyExistsOutput = builder.Output("Contains", typeof(bool));
    }
    
    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken cancellationToken = default)
    {
        var dictionary = ((dynamic)_dictionaryInput).GetValueOrDefault();
        var key = ((dynamic)_keyInput).GetValueOrDefault();
        bool keyExists = false;
        
        if (dictionary == null)
        {
            ((dynamic)_keyExistsOutput).Value = false;
            yield return NotExistsOut;
            yield break;
        }
        
        // 키 존재 여부 확인
        keyExists = dictionary.ContainsKey(key);
        ((dynamic)_keyExistsOutput).Value = keyExists;
        
        // 결과에 따라 적절한 Flow 출력
        if (keyExists)
            yield return ExistsOut;
        else
            yield return NotExistsOut;
    }
} 