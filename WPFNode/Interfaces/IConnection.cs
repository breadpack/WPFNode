using WPFNode.Models;

namespace WPFNode.Interfaces;

public interface IConnection : IJsonSerializable {
    Guid        Guid             { get; }
    IOutputPort  Source         { get; }
    IInputPort Target         { get; }
    PortId SourcePortId { get; }
    PortId TargetPortId { get; }
    void        Disconnect();
}