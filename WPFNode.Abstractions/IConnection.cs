using System;

namespace WPFNode.Abstractions;

public interface IConnection {
    Guid        Id             { get; }
    IOutputPort  Source         { get; }
    IInputPort Target         { get; }
    PortId SourcePortId { get; }
    PortId TargetPortId { get; }
    void        Disconnect();
}