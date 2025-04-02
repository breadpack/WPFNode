using System;

namespace WPFNode.Interfaces;

public interface IInputPort : IPort {
    /// <summary>
    /// 연결된 OutputPort의 데이터 타입입니다. 연결되지 않은 경우 null입니다.
    /// </summary>
    Type? ConnectedType { get; }

    bool        CanAcceptType(Type  type);
    IConnection Connect(IOutputPort source);
    object?     Value { get; }
}

public interface IInputPort<T> : IInputPort {
    T? GetValueOrDefault(T? defaultValue = default);
}