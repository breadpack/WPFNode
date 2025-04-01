using System;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Dictionary.GetOrDefault")]
[NodeDescription("Dictionary에서 키에 해당하는 값을 가져옵니다. 키가 없는 경우 기본값을 반환합니다.")]
[NodeCategory("컬렉션")]
public class DictionaryGetOrDefaultNode : NodeBase
{
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; set; }
    
    [NodeFlowOut("Success")]
    public IFlowOutPort SuccessOut { get; set; }
    
    [NodeFlowOut("Failure")]
    public IFlowOutPort FailureOut { get; set; }
    
    [NodeProperty("Key Type", OnValueChanged = nameof(TypeChanged))]
    public NodeProperty<Type> KeyType { get; set; }
    
    [NodeProperty("Value Type", OnValueChanged = nameof(TypeChanged))]
    public NodeProperty<Type> ValueType { get; set; }
    
    private IInputPort _dictionaryInput;
    private IInputPort _keyInput;
    private IInputPort _defaultValueInput;
    private IOutputPort _valueOutput;
    private IOutputPort _keyExistsOutput;
    
    public DictionaryGetOrDefaultNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Dictionary.GetOrDefault";
        Description = "Dictionary에서 키에 해당하는 값을 가져옵니다. 키가 없는 경우 기본값을 반환합니다.";
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
        _defaultValueInput = builder.Input("DefaultValue", valueType);
        _valueOutput = builder.Output("Value", valueType);
        _keyExistsOutput = builder.Output("Contains", typeof(bool));
    }
    
    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken cancellationToken = default)
    {
        var dictionary = ((dynamic)_dictionaryInput).GetValueOrDefault();
        var key = ((dynamic)_keyInput).GetValueOrDefault();
        var defaultValue = ((dynamic)_defaultValueInput).GetValueOrDefault();
        
        if (dictionary == null)
        {
            ((dynamic)_valueOutput).Value = defaultValue;
            ((dynamic)_keyExistsOutput).Value = false;
            yield return FailureOut;
            yield break;
        }
        
        if (key == null)
        {
            ((dynamic)_valueOutput).Value = defaultValue;
            ((dynamic)_keyExistsOutput).Value = false;
            yield return FailureOut;
            yield break;
        }
        
        var keyExists = dictionary.ContainsKey(key);
        ((dynamic)_valueOutput).Value = keyExists ? dictionary[key] : defaultValue;
        ((dynamic)_keyExistsOutput).Value = keyExists;
        
        yield return keyExists ? SuccessOut : FailureOut;
    }
} 