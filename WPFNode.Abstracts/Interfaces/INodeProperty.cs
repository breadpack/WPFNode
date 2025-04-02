using System.ComponentModel;
using System;
using System.Reflection;
using WPFNode.Attributes; // Added for NodePropertyAttribute
using WPFNode.Models;     // Added for NodeBase

namespace WPFNode.Interfaces;

public interface INodeProperty : INotifyPropertyChanged, IJsonSerializable {
    string  Name             { get; }
    string  DisplayName      { get; }
    string? Format           { get; }
    bool    CanConnectToPort { get; set; }
    Type    PropertyType     { get; }
    Type?   ElementType      { get; }
    bool    IsVisible        { get; }

    // 값 관련
    object?               Value { get; set; }
    public INode Node  { get; }

    /// <summary>
    /// Attribute 및 노드 정보를 기반으로 프로퍼티를 초기화합니다.
    /// </summary>
    /// <param name="node">이 프로퍼티가 속한 노드.</param>
    /// <param name="attribute">이 프로퍼티를 정의하는 NodePropertyAttribute.</param>
    /// <param name="memberInfo">이 프로퍼티에 해당하는 클래스 멤버 정보.</param>
    void Initialize(INode node, NodePropertyAttribute attribute, MemberInfo memberInfo);
}
