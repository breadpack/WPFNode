using System;
using WPFNode.Models;

namespace WPFNode.Interfaces;

public interface IConnection : IJsonSerializable {
    Guid        Guid             { get; }
    IPort  Source         { get; }
    IPort Target         { get; }
    PortId SourcePortId { get; }
    PortId TargetPortId { get; }
    void        Disconnect();
}