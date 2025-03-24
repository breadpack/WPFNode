namespace WPFNode.Interfaces;

public interface IOutputPort : IPort {
    bool        CanConnectTo(IInputPort targetPort);
    object?     Value { get; set; }
    IConnection Connect(IInputPort target);
    void        Disconnect();
    void        Disconnect(IInputPort target);
}