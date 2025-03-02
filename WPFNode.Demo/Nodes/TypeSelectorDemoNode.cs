using System;
using WPFNode.Attributes;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;

namespace WPFNode.Demo.Nodes;

[NodeName("Type Selector Demo")]
[NodeCategory("Demo")]
[NodeDescription("Type 선택 기능을 보여주는 데모 노드입니다.")]
public class TypeSelectorDemoNode : NodeBase
{
    private readonly INodeProperty _selectedType;
    private readonly OutputPort<string> _typeNamePort;
    private readonly OutputPort<string> _fullNamePort;
    private readonly OutputPort<bool> _isValueTypePort;
    private readonly OutputPort<Type> _baseTypePort;
    
    public TypeSelectorDemoNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        _selectedType = CreateProperty<Type>("selectedType", "Selected Type");
        _selectedType.Value = typeof(object);
        
        _typeNamePort = CreateOutputPort<string>("Type Name");
        _fullNamePort = CreateOutputPort<string>("Full Name");
        _isValueTypePort = CreateOutputPort<bool>("Is Value Type");
        _baseTypePort = CreateOutputPort<Type>("Base Type");
    }
    
    protected override Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var type = (Type)_selectedType.Value!;
        _typeNamePort.Value = type.Name;
        _fullNamePort.Value = type.FullName ?? string.Empty;
        _isValueTypePort.Value = type.IsValueType;
        _baseTypePort.Value = type.BaseType;
        return Task.CompletedTask;
    }
} 