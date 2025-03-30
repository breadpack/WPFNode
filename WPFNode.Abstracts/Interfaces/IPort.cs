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
}