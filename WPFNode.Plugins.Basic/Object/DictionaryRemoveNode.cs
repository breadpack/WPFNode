using System;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Dictionary.Remove")]
[NodeDescription("Dictionary에서 지정된 키와 연결된 항목을 삭제합니다.")]
[NodeCategory("컬렉션")]
public class DictionaryRemoveNode : NodeBase
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
    private IOutputPort _updatedDictionaryOutput;
    private IOutputPort _removeSuccessOutput;
    
    public DictionaryRemoveNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Dictionary.Remove";
        Description = "Dictionary에서 지정된 키와 연결된 항목을 삭제합니다.";
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
        _updatedDictionaryOutput = builder.Output("Updated", dictType);
        _removeSuccessOutput = builder.Output("RemoveSuccess", typeof(bool));
    }
    
    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken cancellationToken = default)
    {
        var dictionary = ((dynamic)_dictionaryInput).GetValueOrDefault();
        var key = ((dynamic)_keyInput).GetValueOrDefault();
        bool removeSuccess = false;
        
        if (dictionary == null)
        {
            System.Diagnostics.Debug.WriteLine("DictionaryRemoveNode: Dictionary is null");
            ((dynamic)_removeSuccessOutput).Value = false;
            yield return FailureOut;
            yield break;
        }
        
        // 키가 존재하면 삭제
        if (dictionary.ContainsKey(key))
        {
            dictionary.Remove(key);
            removeSuccess = true;
        }
        
        // 결과 출력
        ((dynamic)_removeSuccessOutput).Value = removeSuccess;
        ((dynamic)_updatedDictionaryOutput).Value = dictionary;
        
        // 성공/실패에 따라 다른 Flow 출력
        if (removeSuccess)
            yield return SuccessOut;
        else
            yield return FailureOut;
    }
} 