using System.ComponentModel;
using WPFNode.Models;

namespace WPFNode.Interfaces;

public interface IPort : INotifyPropertyChanged, IJsonSerializable {
    PortId                     Id          { get; }
    string                     Name        { get; }
    Type                       DataType    { get; }
    bool                       IsInput     { get; }
    bool                       IsConnected { get; }
    bool                       IsVisible   { get; set; }
    IReadOnlyList<IConnection> Connections { get; }
    INode                      Node        { get; }
    void                       AddConnection(IConnection    connection);
    void                       RemoveConnection(IConnection connection);
    int                        GetPortIndex();
    /// <summary>
    /// 다른 포트와 직접 연결합니다.
    /// </summary>
    /// <param name="otherPort">연결할 대상 포트</param>
    /// <returns>생성된 연결 객체</returns>
    IConnection Connect(IPort otherPort);
}

public interface IInputPort : IPort {
    bool        CanAcceptType(Type  type);
    IConnection Connect(IOutputPort source);
    void        Disconnect();
}

public interface IInputPort<T> : IInputPort {
    T? GetValueOrDefault(T? defaultValue = default);
}

public interface IOutputPort : IPort {
    bool        CanConnectTo(IInputPort targetPort);
    object?     Value { get; set; }
    IConnection Connect(IInputPort target);
    void        Disconnect();
    void        Disconnect(IInputPort target);
}