using System;

namespace WPFNode.Interfaces;

public interface IInputPort<T> : IInputPort {
    T? GetValueOrDefault(T? defaultValue = default);
}

public interface IInputPort : IPort {
    bool        CanAcceptType(Type  type);
    IConnection Connect(IOutputPort source);
}