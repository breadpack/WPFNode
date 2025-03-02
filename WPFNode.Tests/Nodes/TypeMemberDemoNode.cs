using System;
using System.Reflection;
using System.ComponentModel;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Linq;
using System.Text.Json;

namespace WPFNode.Plugins.Basic.Nodes;

[NodeName("Type Member Demo")]
[NodeCategory("Demo")]
[NodeDescription("타입의 public 멤버를 NodeProperty로 보여주는 데모 노드입니다.")]
public class TypeMemberDemoNode : NodeBase
{
    private readonly INodeProperty _selectedType;
    private Type? _lastType;
    
    public TypeMemberDemoNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        _selectedType = CreateProperty<Type>("selectedType", "Selected Type");
        _selectedType.Value = typeof(string);
        
        // 타입 변경 감지
        if (_selectedType is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(INodeProperty.Value))
                {
                    UpdateTypeMembers();
                }
            };
        }
        
        UpdateTypeMembers();
    }
    
    private void UpdateTypeMembers(bool preserveProperties = false)
    {
        var type = (Type?)_selectedType.Value;
        if (type == null || type == _lastType) return;

        if (!preserveProperties)
        {
            // selectedType을 제외한 나머지 프로퍼티 제거
            foreach (var property in Properties.Values.ToList())
            {
                if (property != _selectedType)
                {
                    RemoveProperty(property);
                }
            }
        }
        
        // 새 타입의 멤버 추가
        var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => m is PropertyInfo or FieldInfo);
            
        foreach (var member in members)
        {
            // 이미 존재하는 프로퍼티는 건너뛰기
            if (preserveProperties && Properties.ContainsKey(member.Name))
                continue;

            var memberType = member switch
            {
                PropertyInfo prop => prop.PropertyType,
                FieldInfo field => field.FieldType,
                _ => null
            };
            
            if (memberType == null) continue;
            
            CreateProperty(
                member.Name,
                member.Name,
                memberType,
                canConnectToPort: true);
        }
        
        _lastType = type;
    }
    
    protected override Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        // 실제 처리는 하지 않음 (데모용)
        return Task.CompletedTask;
    }

    public override void ReadJson(JsonElement element)
    {
        // selectedType의 값이 복원되기 전의 현재 타입 저장
        var previousType = _selectedType.Value as Type;
        
        base.ReadJson(element);

        // selectedType의 값이 복원된 후 멤버 프로퍼티들을 다시 생성
        if (_selectedType.Value != null)
        {
            var newType = _selectedType.Value as Type;
            // 타입이 변경된 경우에만 UpdateTypeMembers 호출
            if (newType != previousType)
            {
                UpdateTypeMembers(preserveProperties: true);
            }
        }
    }
} 