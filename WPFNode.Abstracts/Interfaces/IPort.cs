using System;
using System.Collections.Generic;
using System.ComponentModel;
using WPFNode.Models;

namespace WPFNode.Interfaces;

public interface IPort : INotifyPropertyChanged, IJsonSerializable {
    PortId Id          { get; }
    string Name        { get; }
    Type   DataType    { get; }
    bool   IsInput     { get; }
    bool   IsConnected { get; }
    bool   IsVisible   { get; set; }
    IReadOnlyList<IConnection> Connections { get; }
    INode                      Node        { get; }
    void                       AddConnection(IConnection    connection);
    void                       RemoveConnection(IConnection connection);
    int                        GetPortIndex();
    void                       Disconnect();

    /// <summary>
    /// 포트가 노드에 완전히 추가되고 다른 모든 포트 구성이 완료된 후 호출됩니다.
    /// 다른 포트 참조, 이벤트 구독 등 지연된 초기화 로직을 수행할 수 있습니다.
    /// </summary>
    void Initialize();
}
