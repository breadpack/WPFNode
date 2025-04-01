using System;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Dictionary.Get")]
[NodeDescription("Dictionary에서 키에 해당하는 값을 가져옵니다.")]
[NodeCategory("컬렉션")]
public class DictionaryGetNode : NodeBase
{
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; set; }
    
    [NodeFlowOut("성공")]
    public IFlowOutPort SuccessOut { get; set; }
    
    [NodeFlowOut("실패")]
    public IFlowOutPort FailureOut { get; set; }
    
    [NodeProperty("키 타입", OnValueChanged = nameof(TypeChanged))]
    public NodeProperty<Type> KeyType { get; set; }
    
    [NodeProperty("값 타입", OnValueChanged = nameof(TypeChanged))]
    public NodeProperty<Type> ValueType { get; set; }
    
    private IInputPort _dictionaryInput;
    private IInputPort _keyInput;
    private IOutputPort _valueOutput;
    private IOutputPort _keyExistsOutput;
    
    public DictionaryGetNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Dictionary.Get";
        Description = "Dictionary에서 키에 해당하는 값을 가져옵니다.";
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
        _valueOutput = builder.Output("Value", valueType);
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
            System.Diagnostics.Debug.WriteLine("DictionaryGetNode: Dictionary is null");
            ((dynamic)_keyExistsOutput).Value = false;
            yield return FailureOut;
            yield break;
        }
        
        // 키가 존재하는지 확인
        keyExists = dictionary.ContainsKey(key);
        ((dynamic)_keyExistsOutput).Value = keyExists;
        
        if (keyExists)
        {
            // 값 출력
            ((dynamic)_valueOutput).Value = dictionary[key];
            yield return SuccessOut;
        }
        else
        {
            ((dynamic)_valueOutput).Value = null;
            yield return FailureOut;
        }
    }
} 