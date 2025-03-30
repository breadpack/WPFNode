using System.ComponentModel;
using System;

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
}