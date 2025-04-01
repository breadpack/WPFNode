using System;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Dictionary.Add")]
[NodeDescription("Dictionary에 키-값 쌍을 추가하거나 업데이트합니다.")]
[NodeCategory("컬렉션")]
public class DictionaryAddNode : NodeBase
{
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; set; }
    
    [NodeFlowOut]
    public IFlowOutPort FlowOut { get; set; }
    
    [NodeProperty("키 타입", OnValueChanged = nameof(TypeChanged))]
    public NodeProperty<Type> KeyType { get; set; }
    
    [NodeProperty("값 타입", OnValueChanged = nameof(TypeChanged))]
    public NodeProperty<Type> ValueType { get; set; }
    
    private IInputPort _dictionaryInput;
    private IInputPort _keyInput;
    private IInputPort _valueInput;
    private IOutputPort _updatedDictionaryOutput;
    
    public DictionaryAddNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Dictionary.Add";
        Description = "Dictionary에 키-값 쌍을 추가하거나 업데이트합니다.";
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
        _valueInput = builder.Input("Value", valueType);
        _updatedDictionaryOutput = builder.Output("Updated", dictType);
    }
    
    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken cancellationToken = default)
    {
        var dictionary = ((dynamic)_dictionaryInput).GetValueOrDefault();
        var key = ((dynamic)_keyInput).GetValueOrDefault();
        var value = ((dynamic)_valueInput).GetValueOrDefault();
        
        if (dictionary == null)
        {
            yield return FlowOut;
            yield break;
        }
        
        try
        {
            // 키가 이미 존재하면 값 업데이트
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                // 키가 없으면 새로 추가
                dictionary.Add(key, value);
            }
            
            // 업데이트된 Dictionary 출력
            ((dynamic)_updatedDictionaryOutput).Value = dictionary;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DictionaryAddNode: 오류 발생 - {ex.Message}");
        }
        
        yield return FlowOut;
    }
} 